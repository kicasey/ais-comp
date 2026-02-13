using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_For_Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NamesController : ControllerBase
{
    private readonly IConfiguration _config;

    public NamesController(IConfiguration config) => _config = config;

    [HttpGet]
    public async Task<IActionResult> GetNames(CancellationToken ct)
    {
        var cs = _config.GetConnectionString("ResumeDb");
        var table = _config["MySQL:ResumePiiTable"] ?? "resume_pii";
        if (string.IsNullOrEmpty(cs)) return Ok(new Dictionary<string, string>());
        var map = new Dictionary<string, string>();
        try
        {
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync(ct);
            var cmd = new MySqlCommand($"SELECT resume_id, candidate_name FROM `{table}`", conn);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var id = r.IsDBNull(0) ? "" : r.GetString(0);
                var name = r.IsDBNull(1) ? "" : r.GetString(1);
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                    map[id] = name;
            }
        }
        catch { /* return empty map */ }
        return Ok(map);
    }
}
