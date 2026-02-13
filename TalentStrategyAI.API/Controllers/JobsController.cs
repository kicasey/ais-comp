using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentStrategyAI.API.Data;
using TalentStrategyAI.API.Models;

namespace TalentStrategyAI.API.Controllers;

/// <summary>
/// Open job postings and recommended employees. Tries resume-api first;
/// then database (create/update/delete); then TestData fallback.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly ILogger<JobsController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _db;

    public JobsController(
        ILogger<JobsController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IWebHostEnvironment env,
        AppDbContext db)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _env = env;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs()
    {
        var path = (_configuration["ResumeApi:JobsPath"] ?? "api/jobs").TrimStart('/');
        var (success, data) = await TryGetFromApiAsync<JobDto[]>(path);
        if (success && data != null)
            return Ok(data);
        var dbJobs = await _db.Jobs.OrderBy(j => j.Id).ToListAsync();
        if (dbJobs.Count > 0)
            return Ok(dbJobs.Select(j => JobToDto(j)).ToArray());
        return Ok(await GetJobsFromTestDataAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(string id)
    {
        var jobsPath = _configuration["ResumeApi:JobsPath"] ?? "api/jobs";
        var path = $"{jobsPath.TrimEnd('/')}/{Uri.EscapeDataString(id)}";
        var (success, data) = await TryGetFromApiAsync<JobDetailDto>(path);
        if (success && data != null)
        {
            return Ok(data);
        }
        if (TryParseJobId(id, out var dbId))
        {
            var job = await _db.Jobs.FindAsync(dbId);
            if (job != null)
                return Ok(JobToDetailDto(job));
        }
        var testJob = await GetJobByIdFromTestDataAsync(id);
        if (testJob == null)
            return NotFound(new { message = "Job not found." });
        return Ok(testJob);
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] JobCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Title))
            return BadRequest(new { message = "Title is required." });
        var job = new Job
        {
            Title = request.Title.Trim(),
            Department = (request.Department ?? "").Trim(),
            Location = (request.Location ?? "").Trim(),
            Description = (request.Description ?? "").Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Created job {JobId} ({Title})", job.Id, job.Title);
        await SyncJobToResumeApiAsync(JobToDetailDto(job), method: "POST", path: null);
        return CreatedAtAction(nameof(GetJob), new { id = JobIdString(job.Id) }, JobToDetailDto(job));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJob(string id, [FromBody] JobCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Title))
            return BadRequest(new { message = "Title is required." });
        if (!TryParseJobId(id, out var dbId))
            return NotFound(new { message = "Job not found." });
        var job = await _db.Jobs.FindAsync(dbId);
        if (job == null)
            return NotFound(new { message = "Job not found." });
        job.Title = request.Title.Trim();
        job.Department = (request.Department ?? "").Trim();
        job.Location = (request.Location ?? "").Trim();
        job.Description = (request.Description ?? "").Trim();
        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated job {JobId}", job.Id);
        await SyncJobToResumeApiAsync(JobToDetailDto(job), method: "PUT", path: JobIdString(job.Id));
        return Ok(JobToDetailDto(job));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(string id)
    {
        if (!TryParseJobId(id, out var dbId))
            return NotFound(new { message = "Job not found." });
        var job = await _db.Jobs.FindAsync(dbId);
        if (job == null)
            return NotFound(new { message = "Job not found." });
        var idStr = JobIdString(job.Id);
        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Deleted job {JobId} ({Title})", job.Id, job.Title);
        await SyncJobToResumeApiAsync(null, method: "DELETE", path: idStr);
        return NoContent();
    }

    [HttpGet("{jobId}/recommendations")]
    public async Task<IActionResult> GetRecommendations(string jobId)
    {
        _logger.LogInformation("Recommendations requested for job {JobId}", jobId);
        var jobsPath = _configuration["ResumeApi:RecommendationsPath"] ?? _configuration["ResumeApi:JobsPath"] ?? "api/jobs";
        var path = $"{jobsPath.TrimEnd('/')}/{Uri.EscapeDataString(jobId)}/recommendations";
        var (success, data) = await TryGetFromApiAsync<EmployeeMatchDto[]>(path);
        if (success && data != null)
        {
            return Ok(data);
        }
        return Ok(Array.Empty<EmployeeMatchDto>());
    }

    private static bool TryParseJobId(string id, out int dbId)
    {
        dbId = 0;
        if (string.IsNullOrEmpty(id)) return false;
        var s = id.TrimStart();
        if (s.StartsWith("job-", StringComparison.OrdinalIgnoreCase))
            s = s.Substring(4);
        return int.TryParse(s, out dbId) && dbId > 0;
    }

    private static string JobIdString(int id) => "job-" + id;

    private static JobDto JobToDto(Job j) => new JobDto
    {
        Id = JobIdString(j.Id),
        Title = j.Title,
        Department = j.Department,
        Location = j.Location
    };

    private static JobDetailDto JobToDetailDto(Job j) => new JobDetailDto
    {
        Id = JobIdString(j.Id),
        Title = j.Title,
        Department = j.Department,
        Location = j.Location,
        Description = j.Description
    };

    private async Task<(bool Success, T? Data)> TryGetFromApiAsync<T>(string path) where T : class
    {
        var baseUrl = _configuration["ResumeApi:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)) return (false, null);

        try
        {
            var client = _httpClientFactory.CreateClient("ResumeApi");
            path = path.TrimStart('/');
            var response = await client.GetAsync(path);
            if (!response.IsSuccessStatusCode) return (false, null);
            var body = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<T>(body, options);
            return (data != null, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Resume API request failed for {Path}", path);
            return (false, null);
        }
    }

    private async Task SyncJobToResumeApiAsync(JobDetailDto? job, string method, string? path)
    {
        var baseUrl = _configuration["ResumeApi:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)) return;
        var jobsPath = (_configuration["ResumeApi:JobsPath"] ?? "api/jobs").TrimStart('/');
        var fullPath = string.IsNullOrEmpty(path) ? jobsPath : $"{jobsPath.TrimEnd('/')}/{Uri.EscapeDataString(path)}";
        try
        {
            var client = _httpClientFactory.CreateClient("ResumeApi");
            HttpResponseMessage response;
            if (method == "POST" && job != null)
            {
                var payload = new { job.Title, job.Department, job.Location, job.Description };
                response = await client.PostAsJsonAsync(fullPath, payload);
            }
            else if (method == "PUT" && job != null)
            {
                var payload = new { job.Title, job.Department, job.Location, job.Description };
                response = await client.PutAsJsonAsync(fullPath, payload);
            }
            else if (method == "DELETE")
            {
                response = await client.DeleteAsync(fullPath);
            }
            else
                return;
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Resume API {Method} {Path} returned {Code}", method, fullPath, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Resume API {Method} {Path} failed", method, fullPath);
        }
    }

    private async Task<JobDto[]> GetJobsFromTestDataAsync()
    {
        var (jobs, _, _) = await LoadJobsAndRecommendationsAsync();
        return jobs != null && jobs.Count > 0 ? jobs.ToArray() : GetFallbackJobs();
    }

    private async Task<JobDetailDto?> GetJobByIdFromTestDataAsync(string id)
    {
        var (_, jobDetails, _) = await LoadJobsAndRecommendationsAsync();
        var job = FindJobDetail(jobDetails, id);
        if (job != null) return job;
        var fallback = GetFallbackJobs().FirstOrDefault(j => string.Equals(j.Id, id, StringComparison.OrdinalIgnoreCase));
        if (fallback != null)
            return new JobDetailDto { Id = fallback.Id, Title = fallback.Title, Department = fallback.Department, Location = fallback.Location, Description = "" };
        return null;
    }

    private async Task<EmployeeMatchDto[]> GetRecommendationsFromTestDataAsync(string jobId)
    {
        var (_, _, recommendationsByJob) = await LoadJobsAndRecommendationsAsync();
        if (recommendationsByJob != null && recommendationsByJob.TryGetValue(jobId, out var list) && list != null && list.Count > 0)
        {
            return list.Select(r => new EmployeeMatchDto
            {
                EmployeeId = r.EmployeeId,
                Name = r.Name,
                ConfidencePercent = r.ConfidencePercent
            }).ToArray();
        }
        return GetFallbackRecommendations();
    }

    private async Task<(List<JobDto>? Jobs, List<JobDetailDto>? JobDetails, Dictionary<string, List<EmployeeMatchDto>>? RecommendationsByJob)> LoadJobsAndRecommendationsAsync()
    {
        var baseDir = AppContext.BaseDirectory;
        var testDataDir = Path.Combine(baseDir, "TestData");
        if (!Directory.Exists(testDataDir))
            testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

        var jobsPath = Path.Combine(testDataDir, "sample-jobs.json");
        var recsPath = Path.Combine(testDataDir, "sample-recommendations.json");

        List<JobDto>? jobs = null;
        List<JobDetailDto>? jobDetails = null;
        Dictionary<string, List<EmployeeMatchDto>>? recsByJob = null;

        if (System.IO.File.Exists(jobsPath))
        {
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(jobsPath);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("jobs", out var arr))
                {
                    jobs = new List<JobDto>();
                    jobDetails = new List<JobDetailDto>();
                    foreach (var j in arr.EnumerateArray())
                    {
                        var id = j.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                        var title = j.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                        var dept = j.TryGetProperty("department", out var d) ? d.GetString() ?? "" : "";
                        var loc = j.TryGetProperty("location", out var locProp) ? locProp.GetString() ?? "" : "";
                        var desc = j.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";
                        jobs.Add(new JobDto { Id = id, Title = title, Department = dept, Location = loc });
                        jobDetails.Add(new JobDetailDto { Id = id, Title = title, Department = dept, Location = loc, Description = desc });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load sample-jobs.json");
            }
        }

        if (System.IO.File.Exists(recsPath))
        {
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(recsPath);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("recommendationsByJob", out var byJob))
                {
                    recsByJob = new Dictionary<string, List<EmployeeMatchDto>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in byJob.EnumerateObject())
                    {
                        var list = new List<EmployeeMatchDto>();
                        foreach (var item in prop.Value.EnumerateArray())
                        {
                            var empId = item.TryGetProperty("employeeId", out var eid) ? eid.GetString() ?? "" : "";
                            var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                            var pct = item.TryGetProperty("confidencePercent", out var p) ? p.GetInt32() : 0;
                            list.Add(new EmployeeMatchDto { EmployeeId = empId, Name = name, ConfidencePercent = pct });
                        }
                        recsByJob[prop.Name] = list;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load sample-recommendations.json");
            }
        }

        return (jobs, jobDetails, recsByJob);
    }

    private static JobDetailDto? FindJobDetail(List<JobDetailDto>? list, string id)
    {
        if (list == null) return null;
        return list.FirstOrDefault(j => string.Equals(j.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private static JobDto[] GetFallbackJobs()
    {
        return new[]
        {
            new JobDto { Id = "job-1", Title = "Senior Consultant – Technology", Department = "Technology", Location = "New York" },
            new JobDto { Id = "job-2", Title = "Manager – Assurance", Department = "Assurance", Location = "Chicago" },
            new JobDto { Id = "job-3", Title = "Staff – Tax", Department = "Tax", Location = "Dallas" },
            new JobDto { Id = "job-4", Title = "Senior Analyst – Consulting", Department = "Consulting", Location = "Remote" },
            new JobDto { Id = "job-5", Title = "Technology Consultant – Data & Analytics", Department = "Technology", Location = "New York" },
        };
    }

    private static EmployeeMatchDto[] GetFallbackRecommendations()
    {
        return new[]
        {
            new EmployeeMatchDto { EmployeeId = "emp-1", Name = "Alex Chen", ConfidencePercent = 92 },
            new EmployeeMatchDto { EmployeeId = "emp-2", Name = "Jordan Smith", ConfidencePercent = 87 },
            new EmployeeMatchDto { EmployeeId = "emp-3", Name = "Sam Williams", ConfidencePercent = 78 },
        };
    }

    public class JobDto
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Department { get; set; } = "";
        public string Location { get; set; } = "";
        public string? Description { get; set; }
    }

    public class JobDetailDto : JobDto
    {
        public new string Description { get; set; } = "";
    }

    public class EmployeeMatchDto
    {
        public string EmployeeId { get; set; } = "";
        public string Name { get; set; } = "";
        public int ConfidencePercent { get; set; }
    }

    public class JobCreateRequest
    {
        public string? Title { get; set; }
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
    }
}
