using Microsoft.AspNetCore.Mvc;
using API_For_Server.Services;

namespace API_For_Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IWebhookProxyService _proxy;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IWebhookProxyService proxy, ILogger<ChatController> logger)
    {
        _proxy = proxy;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(1 * 1024 * 1024)]
    public async Task<IActionResult> Post(CancellationToken ct)
    {
        try
        {
            var response = await _proxy.ForwardToChatAsync(Request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("Chat webhook returned {StatusCode}", response.StatusCode);
            if (string.IsNullOrEmpty(body))
                return StatusCode((int)response.StatusCode);
            try
            {
                return StatusCode((int)response.StatusCode, System.Text.Json.JsonSerializer.Deserialize<object>(body));
            }
            catch
            {
                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    Content = body,
                    ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat webhook error");
            return StatusCode(502, new { message = "Upstream webhook failed.", detail = ex.Message });
        }
    }
}
