using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace TalentStrategyAI.API.Controllers;

/// <summary>
/// Chat API for AI assistant. Proxies to resume-api.campbellthompson.com when configured.
/// If the API is unavailable or returns an error, returns a fallback message so the UI still works.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ChatController(
        ILogger<ChatController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Preset) && string.IsNullOrWhiteSpace(request?.CustomText))
        {
            return BadRequest(new { message = "Preset or CustomText is required." });
        }

        _logger.LogInformation("Chat preset: {Preset}, JobId: {JobId}, EmployeeId: {EmployeeId}", request.Preset, request.JobId, request.EmployeeId);

        var baseUrl = _configuration["ResumeApi:BaseUrl"];
        var chatPath = _configuration["ResumeApi:ChatPath"] ?? "api/chat";

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ResumeApi");
                var payload = new
                {
                    preset = request.Preset,
                    customText = request.CustomText,
                    jobId = request.JobId,
                    employeeId = request.EmployeeId,
                    userEmail = request.UserEmail,
                    userName = request.UserName,
                    userId = request.UserId
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var path = chatPath.TrimStart('/');
                var response = await client.PostAsync(path, content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Resume API response body (first 2000 chars): {Body}", body?.Length > 2000 ? body[..2000] : body);
                    if (!string.IsNullOrEmpty(body))
                    {
                        // Pass through the raw JSON so structured data (top_candidates, etc.) is preserved
                        try
                        {
                            var parsed = JsonSerializer.Deserialize<JsonElement>(body);
                            return Ok(parsed);
                        }
                        catch
                        {
                            return Ok(new ChatResponse { Response = body });
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Resume API returned {StatusCode} for chat", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resume API chat call failed");
            }
        }

        var fallback = GetFallbackResponse(request);
        return Ok(new ChatResponse { Response = fallback });
    }

    private static string GetFallbackResponse(ChatRequest request)
    {
        if (string.Equals(request.Preset, "explain_employee_match", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(request.JobId)
            && !string.IsNullOrWhiteSpace(request.EmployeeId))
        {
            return "**Match explanation (demo)**\n\nThis employee is a strong match for this role based on skills and experience in our database. " +
                   "When the resume-api is connected, AI will provide a detailed explanation of strengths and gaps.\n\n" +
                   "**Upskilling suggestions:**\n• Complete relevant EY learning modules for the role.\n• Shadow a current team member in this function.\n• Consider certification or training in key areas identified in the job description.";
        }
        return "The AI assistant could not be reached. Recommendations below may show sample or saved data instead.";
    }

    private static string? TryParseResponseText(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            foreach (var key in new[] { "response", "message", "text", "content" })
            {
                if (root.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    var s = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) return s;
                }
            }
            return root.GetString();
        }
        catch
        {
            return json;
        }
    }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public class ChatRequest
    {
        public string? Preset { get; set; }
        public string? CustomText { get; set; }
        public string? JobId { get; set; }
        public string? EmployeeId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public string? UserId { get; set; }
    }

    public class ChatResponse
    {
        public string? Response { get; set; }
    }
}
