using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentStrategyAI.API.Data;
using TalentStrategyAI.API.Services;

namespace TalentStrategyAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private static readonly string[] EmployeePresets = { "match_roles", "explain_match", "suggest_upskill", "match_summary" };

    private readonly ILogger<ChatController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _db;
    private readonly IResumeTextService _resumeText;
    private readonly IWebHostEnvironment _env;

    public ChatController(
        ILogger<ChatController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        AppDbContext db,
        IResumeTextService resumeText,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _db = db;
        _resumeText = resumeText;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Preset) && string.IsNullOrWhiteSpace(request?.CustomText))
            return BadRequest(new { message = "Preset or CustomText is required." });

        var preset = (request.Preset ?? "").Trim().ToLowerInvariant();
        var isEmployeePreset = EmployeePresets.Contains(preset);

        if (isEmployeePreset)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Ok(new ChatResponse { Response = "Please sign in to use this feature." });

            var user = await _db.Users.FindAsync(userId);
            if (user == null || user.Role != "employee")
                return Ok(new ChatResponse { Response = "This feature is for employees. Sign in with an employee account." });

            var profile = await _db.EmployeeProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null || string.IsNullOrWhiteSpace(profile.ResumeFilePath))
                return Ok(new ChatResponse { Response = "Please upload your resume in your profile first. Go to the Resume section above and upload a PDF, DOC, or DOCX file. Then try again." });

            var resumeContent = _resumeText.GetResumeText(_env.ContentRootPath, profile.ResumeFilePath);
            var employeeIdForApi = profile.Id.ToString();

            var baseUrl = _configuration["ResumeApi:BaseUrl"];
            var chatPath = _configuration["ResumeApi:ChatPath"] ?? "api/chat";
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("ResumeApi");
                    var payload = new Dictionary<string, object?>
                    {
                        ["preset"] = request.Preset,
                        ["customText"] = request.CustomText,
                        ["jobId"] = request.JobId,
                        ["employeeId"] = employeeIdForApi
                    };
                    if (!string.IsNullOrWhiteSpace(resumeContent))
                        payload["resumeContent"] = resumeContent;
                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var path = chatPath.TrimStart('/');
                    var response = await client.PostAsync(path, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        var responseText = TryParseResponseText(body);
                        if (!string.IsNullOrEmpty(responseText))
                            return Ok(new ChatResponse { Response = responseText });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Resume API chat call failed");
                }
            }

            return Ok(new ChatResponse
            {
                Response = "Your resume is on file and linked to your profile. When the resume-api is connected, the assistant will use it to match you to roles and suggest upskilling. For now, you can try again once the API is available."
            });
        }

        _logger.LogInformation("Chat preset: {Preset}, JobId: {JobId}, EmployeeId: {EmployeeId}", request.Preset, request.JobId, request.EmployeeId);
        var baseUrl2 = _configuration["ResumeApi:BaseUrl"];
        var chatPath2 = _configuration["ResumeApi:ChatPath"] ?? "api/chat";
        if (!string.IsNullOrWhiteSpace(baseUrl2))
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ResumeApi");
                var payload = new
                {
                    preset = request.Preset,
                    customText = request.CustomText,
                    jobId = request.JobId,
                    employeeId = request.EmployeeId
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var path = chatPath2.TrimStart('/');
                var response = await client.PostAsync(path, content);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var responseText = TryParseResponseText(body);
                    if (!string.IsNullOrEmpty(responseText))
                        return Ok(new ChatResponse { Response = responseText });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resume API chat call failed");
            }
        }

        return Ok(new ChatResponse { Response = GetFallbackResponse(request) });
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
        return "The AI assistant could not be reached. Check that ResumeApi:BaseUrl points to resume-api and that the API is running.";
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

    public class ChatRequest
    {
        public string? Preset { get; set; }
        public string? CustomText { get; set; }
        public string? JobId { get; set; }
        public string? EmployeeId { get; set; }
    }

    public class ChatResponse
    {
        public string? Response { get; set; }
    }
}
