using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace TalentStrategyAI.API.Controllers;

/// <summary>
/// Employee chat API. Proxies to resume-api api/employee-chat when ResumeApi:BaseUrl is set.
/// Same request/response shape as ChatController; uses employee-chat webhook (e.g. N8N) for employee-facing presets.
/// </summary>
[ApiController]
[Route("api/employee-chat")]
public class EmployeeChatController : ControllerBase
{
    private readonly ILogger<EmployeeChatController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public EmployeeChatController(
        ILogger<EmployeeChatController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] EmployeeChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Preset) && string.IsNullOrWhiteSpace(request?.CustomText))
        {
            return BadRequest(new { message = "Preset or CustomText is required." });
        }

        _logger.LogInformation("Employee chat preset: {Preset}", request.Preset);

        var baseUrl = _configuration["ResumeApi:BaseUrl"];
        var employeeChatPath = _configuration["ResumeApi:EmployeeChatPath"] ?? "api/employee-chat";

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ResumeApi");
                var payload = new
                {
                    preset = request.Preset,
                    customText = request.CustomText,
                    userEmail = request.UserEmail,
                    userName = request.UserName,
                    userId = request.UserId
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var path = employeeChatPath.TrimStart('/');
                var response = await client.PostAsync(path, content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(body))
                    {
                        try
                        {
                            var parsed = JsonSerializer.Deserialize<JsonElement>(body);
                            return Ok(parsed);
                        }
                        catch
                        {
                            return Ok(new EmployeeChatResponse { Response = body });
                        }
                    }
                    return Ok(new EmployeeChatResponse { Response = "The assistant didn't return a response. Please try again." });
                }
                _logger.LogWarning("Resume API employee-chat returned {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resume API employee-chat call failed");
            }
        }

        return Ok(new EmployeeChatResponse
        {
            Response = "The employee assistant could not be reached. Check that ResumeApi:BaseUrl is set and resume-api is running (with employee-chat webhook configured)."
        });
    }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public class EmployeeChatRequest
    {
        public string? Preset { get; set; }
        public string? CustomText { get; set; }
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public string? UserId { get; set; }
    }

    public class EmployeeChatResponse
    {
        public string? Response { get; set; }
    }
}
