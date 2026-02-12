using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TalentStrategyAI.API.Controllers;

/// <summary>
/// Open job postings and recommended employees. Tries resume-api first;
/// if the API is not configured or fails, returns data from TestData (sample-jobs.json, sample-recommendations.json).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly ILogger<JobsController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public JobsController(
        ILogger<JobsController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs()
    {
        var path = (_configuration["ResumeApi:JobsPath"] ?? "api/jobs").TrimStart('/');
        var (success, data) = await TryGetFromApiAsync<JobDto[]>(path);
        if (success && data != null && data.Length > 0)
        {
            return Ok(data);
        }
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
        var job = await GetJobByIdFromTestDataAsync(id);
        if (job == null)
            return NotFound(new { message = "Job not found." });
        return Ok(job);
    }

    [HttpGet("{jobId}/recommendations")]
    public async Task<IActionResult> GetRecommendations(string jobId)
    {
        _logger.LogInformation("Recommendations requested for job {JobId}", jobId);
        var jobsPath = _configuration["ResumeApi:RecommendationsPath"] ?? _configuration["ResumeApi:JobsPath"] ?? "api/jobs";
        var path = $"{jobsPath.TrimEnd('/')}/{Uri.EscapeDataString(jobId)}/recommendations";
        var (success, data) = await TryGetFromApiAsync<EmployeeMatchDto[]>(path);
        if (success && data != null && data.Length > 0)
        {
            return Ok(data);
        }
        var list = await GetRecommendationsFromTestDataAsync(jobId);
        return Ok(list);
    }

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
}
