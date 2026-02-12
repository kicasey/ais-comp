using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_For_Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IConfiguration config, ILogger<JobsController> logger)
    {
        _config = config;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs(CancellationToken ct)
    {
        var list = await QueryJobsAsync(ct);
        if (list == null) return Ok(Array.Empty<JobDto>());
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(string id, CancellationToken ct)
    {
        var list = await QueryJobsAsync(ct);
        var job = list?.FirstOrDefault(j => string.Equals(j.Id, id, StringComparison.OrdinalIgnoreCase));
        if (job == null) return NotFound(new { message = "Job not found." });
        return Ok(job);
    }

    [HttpGet("{jobId}/recommendations")]
    public async Task<IActionResult> GetRecommendations(string jobId, CancellationToken ct)
    {
        var list = await QueryRecommendationsAsync(jobId, ct);
        if (list == null) return Ok(Array.Empty<EmployeeMatchDto>());
        return Ok(list);
    }

    private async Task<List<JobDto>?> QueryJobsAsync(CancellationToken ct)
    {
        var cs = _config.GetConnectionString("ResumeDb");
        var table = _config["MySQL:JobsTable"] ?? "jobs";
        if (string.IsNullOrEmpty(cs)) { _logger.LogWarning("ResumeDb connection string not set"); return null; }

        var list = new List<JobDto>();
        try
        {
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync(ct);
            var cmd = new MySqlCommand($"SELECT id, title, department, location, description FROM `{table}` ORDER BY id", conn);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                list.Add(new JobDto
                {
                    Id = r.GetString(0),
                    Title = r.IsDBNull(1) ? "" : r.GetString(1),
                    Department = r.IsDBNull(2) ? "" : r.GetString(2),
                    Location = r.IsDBNull(3) ? "" : r.GetString(3),
                    Description = r.IsDBNull(4) ? "" : r.GetString(4)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MySQL jobs query failed");
            return null;
        }
        return list;
    }

    private async Task<List<EmployeeMatchDto>?> QueryRecommendationsAsync(string jobId, CancellationToken ct)
    {
        var cs = _config.GetConnectionString("ResumeDb");
        var recTable = _config["MySQL:RecommendationsTable"] ?? "job_recommendations";
        var piiTable = _config["MySQL:ResumePiiTable"] ?? "resume_pii";
        if (string.IsNullOrEmpty(cs)) { _logger.LogWarning("ResumeDb connection string not set"); return null; }

        var list = new List<EmployeeMatchDto>();
        try
        {
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync(ct);
            // job_recommendations(job_id, resume_id, score) + resume_pii(resume_id, candidate_name)
            var sql = $@"SELECT r.resume_id, COALESCE(p.candidate_name, r.resume_id) AS candidate_name, COALESCE(r.score, 0)
FROM `{recTable}` r
LEFT JOIN `{piiTable}` p ON p.resume_id = r.resume_id
WHERE r.job_id = @jobId
ORDER BY r.score DESC";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@jobId", jobId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new EmployeeMatchDto
                {
                    EmployeeId = reader.GetString(0),
                    Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    ConfidencePercent = reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MySQL recommendations query failed for job {JobId}", jobId);
            return null;
        }
        return list;
    }

    public class JobDto
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Department { get; set; } = "";
        public string Location { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class EmployeeMatchDto
    {
        public string EmployeeId { get; set; } = "";
        public string Name { get; set; } = "";
        public int ConfidencePercent { get; set; }
    }
}
