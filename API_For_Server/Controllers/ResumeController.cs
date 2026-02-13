using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace API_For_Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumeController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ResumeController> _logger;

    private static readonly HashSet<string> ConvertibleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".odt", ".rtf"
    };

    public ResumeController(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<ResumeController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(CancellationToken ct)
    {
        if (!Request.HasFormContentType)
            return BadRequest(new { message = "Expect multipart/form-data (resume file and candidateName)." });

        var form = await Request.ReadFormAsync(ct);
        var file = form.Files.GetFile("resume");
        var candidateName = form["candidateName"].ToString().Trim();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No resume file provided." });

        // 1. Convert to PDF if needed
        byte[] fileBytes;
        string fileName;
        if (NeedsConversion(file))
        {
            _logger.LogInformation("Converting {FileName} to PDF via Gotenberg", file.FileName);
            var converted = await ConvertToPdfAsync(file, ct);
            if (converted == null)
                return StatusCode(502, new { message = "Failed to convert document to PDF." });
            fileBytes = converted;
            fileName = Path.GetFileNameWithoutExtension(file.FileName) + ".pdf";
        }
        else
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            fileBytes = ms.ToArray();
            fileName = file.FileName;
        }

        // 2. Save to incoming-resumes directory
        var saveDir = _config["Resume:SavePath"] ?? "/data/incoming-resumes";
        try
        {
            Directory.CreateDirectory(saveDir);
            var savePath = Path.Combine(saveDir, $"{Guid.NewGuid()}_{fileName}");
            await System.IO.File.WriteAllBytesAsync(savePath, fileBytes, ct);
            _logger.LogInformation("Resume saved to {Path} ({Bytes} bytes)", savePath, fileBytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save resume to {Dir}", saveDir);
            return StatusCode(500, new { message = "Failed to save resume file.", detail = ex.Message });
        }

        // 3. Trigger n8n webhook with metadata only
        var webhookUrl = _config["Webhooks:Resumes"];
        if (string.IsNullOrEmpty(webhookUrl))
            return StatusCode(503, new { message = "Webhooks:Resumes not configured." });

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            var payload = new { candidateName, fileName, originalFileName = file.FileName };
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(webhookUrl, jsonContent, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("Resumes webhook returned {StatusCode}", response.StatusCode);

            if (string.IsNullOrEmpty(body))
                return Ok(new { message = "Resume uploaded successfully." });
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
            _logger.LogError(ex, "Resumes webhook trigger error");
            // File was saved successfully, webhook just failed to trigger
            return Ok(new { message = "Resume saved but workflow trigger failed.", detail = ex.Message });
        }
    }

    private static bool NeedsConversion(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        return !string.IsNullOrEmpty(ext) && ConvertibleExtensions.Contains(ext);
    }

    private async Task<byte[]?> ConvertToPdfAsync(IFormFile file, CancellationToken ct)
    {
        var gotenbergUrl = _config["Gotenberg:BaseUrl"];
        if (string.IsNullOrEmpty(gotenbergUrl)) return null;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            content.Add(streamContent, "files", file.FileName);

            var response = await client.PostAsync($"{gotenbergUrl.TrimEnd('/')}/forms/libreoffice/convert", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gotenberg returned {StatusCode}", response.StatusCode);
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gotenberg conversion error");
            return null;
        }
    }
}
