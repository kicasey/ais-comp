using Microsoft.AspNetCore.Mvc;

namespace TalentStrategyAI.API.Controllers;

/// <summary>
/// Chat API for AI assistant. Frontend sends preset actions (e.g. match_roles, match_summary).
/// This controller will proxy requests to resume-api.campbellthompson.com (which handles SQL + AI).
/// For now returns a stub response so the UI works until the integration is wired.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;

    public ChatController(ILogger<ChatController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Preset))
        {
            return BadRequest(new { message = "Preset is required." });
        }

        _logger.LogInformation("Chat preset requested: {Preset}", request.Preset);

        // Stub: replace with call to resume-api.campbellthompson.com when ready.
        // That API handles SQL and AI together (jobs, matches, explanations, upskilling, bias).
        var response = new ChatResponse
        {
            Response = "This response is a placeholder. Connect this endpoint to resume-api.campbellthompson.com to return real AI and match data from SQL."
        };

        return Ok(response);
    }

    public class ChatRequest
    {
        public string? Preset { get; set; }
    }

    public class ChatResponse
    {
        public string? Response { get; set; }
    }
}
