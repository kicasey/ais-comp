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

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] JobDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Title)) return BadRequest(new { message = "Title is required." });
        var cs = _config.GetConnectionString("ResumeDb");
        var table = _config["MySQL:JobsTable"] ?? "jobs";
        if (string.IsNullOrEmpty(cs)) return StatusCode(500, new { message = "ResumeDb connection string not set." });
        try
        {
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync(ct);
            var cmd = new MySqlCommand(
                $"INSERT INTO `{table}` (title, department, location, description) VALUES (@title, @dept, @loc, @desc); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@title", request.Title.Trim());
            cmd.Parameters.AddWithValue("@dept", (request.Department ?? "").Trim());
            cmd.Parameters.AddWithValue("@loc", (request.Location ?? "").Trim());
            cmd.Parameters.AddWithValue("@desc", (request.Description ?? "").Trim());
            var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
            _logger.LogInformation("Created job {JobId} in resume_ai", newId);
            return CreatedAtAction(nameof(GetJob), new { id = "job-" + newId }, new JobDto
            {
                Id = "job-" + newId,
                Title = request.Title.Trim(),
                Department = (request.Department ?? "").Trim(),
                Location = (request.Location ?? "").Trim(),
                Description = (request.Description ?? "").Trim()
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MySQL job insert failed");
            return StatusCode(500, new { message = "Failed to create job." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJob(string id, [FromBody] JobDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Title)) return BadRequest(new { message = "Title is required." });
        if (!TryParseJobId(id, out var dbId)) return NotFound(new { message = "Job not found." });
        var cs = _config.GetConnectionString("ResumeDb");
        var table = _config["MySQL:JobsTable"] ?? "jobs";
        if (string.IsNullOrEmpty(cs)) return StatusCode(500, new { message = "ResumeDb connection string not set." });
        try
        {
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync(ct);
            var cmd = new MySqlCommand(
                $"UPDATE `{table}` SET title=@title, department=@dept, location=@loc, description=@desc WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", dbId);
            cmd.Parameters.AddWithValue("@title", request.Title.Trim());
            cmd.Parameters.AddWithValue("@dept", (request.Department ?? "").Trim());
            cmd.Parameters.AddWithValue("@loc", (request.Location ?? "").Trim());
            cmd.Parameters.AddWithValue("@desc", (request.Description ?? "").Trim());
            var updated = await cmd.ExecuteNonQueryAsync(ct);
            if (updated == 0) return NotFound(new { message = "Job not found." });
            _logger.LogInformation("Updated job {JobId} in resume_ai", dbId);
            return Ok(new JobDto { Id = "job-" + dbId, Title = request.Title.Trim(), Department = (request.Department ?? "").Trim(), Location = (request.Location ?? "").Trim(), Description = (request.Description ?? "").Trim() });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MySQL job update failed");
            return StatusCode(500, new { message = "Failed to update job." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(string id, CancellationToken ct)
    {
        if (!TryParseJobId(id, out var dbId)) return NotFound(new { message = "Job not found." });
        var cs = _config.GetConnectionString("ResumeDb");
        var table = _config["MySQL:JobsTable"] ?? "jobs";
        if (string.IsNullOrEmpty(cs)) return StatusCode(500, new { message = "ResumeDb connection string not set." });
        try
        {
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync(ct);
            var cmd = new MySqlCommand($"DELETE FROM `{table}` WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", dbId);
            var deleted = await cmd.ExecuteNonQueryAsync(ct);
            if (deleted == 0) return NotFound(new { message = "Job not found." });
            _logger.LogInformation("Deleted job {JobId} from resume_ai", dbId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MySQL job delete failed");
            return StatusCode(500, new { message = "Failed to delete job." });
        }
    }

    private static bool TryParseJobId(string id, out long dbId)
    {
        dbId = 0;
        if (string.IsNullOrWhiteSpace(id)) return false;
        var s = id.Trim();
        if (s.StartsWith("job-", StringComparison.OrdinalIgnoreCase)) s = s.Substring(4);
        return long.TryParse(s, out dbId) && dbId > 0;
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
                var id = r.GetInt32(0);
                list.Add(new JobDto
                {
                    Id = "job-" + id,
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
