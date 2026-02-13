using Microsoft.AspNetCore.Mvc;

namespace TalentStrategyAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NamesController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public NamesController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var baseUrl = _configuration["ResumeApi:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return Ok(new Dictionary<string, string>());

        try
        {
            var client = _httpClientFactory.CreateClient("ResumeApi");
            var response = await client.GetAsync("api/names", ct);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                return Content(json, "application/json");
            }
        }
        catch { /* fall through */ }

        return Ok(new Dictionary<string, string>());
    }
}
