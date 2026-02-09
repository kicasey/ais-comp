using Microsoft.AspNetCore.Mvc;

namespace TalentStrategyAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumeController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ResumeController> _logger;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx" };

    public ResumeController(IWebHostEnvironment environment, ILogger<ResumeController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadResume([FromForm] IFormFile resume, [FromForm] string candidateName)
    {
        try
        {
            // Validate file
            if (resume == null || resume.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            // Validate file size
            if (resume.Length > MaxFileSize)
            {
                return BadRequest(new { message = "File size exceeds 10MB limit." });
            }

            // Validate file extension
            var fileExtension = Path.GetExtension(resume.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "Invalid file type. Only PDF, DOC, and DOCX files are allowed." });
            }

            // Validate candidate name
            if (string.IsNullOrWhiteSpace(candidateName))
            {
                return BadRequest(new { message = "Candidate name is required." });
            }

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_environment.ContentRootPath, "Uploads", "Resumes");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await resume.CopyToAsync(stream);
            }

            _logger.LogInformation($"Resume uploaded successfully: {fileName} for candidate {candidateName}");

            return Ok(new
            {
                message = "Resume uploaded successfully.",
                fileName = fileName,
                candidateName = candidateName,
                fileSize = resume.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading resume: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            return StatusCode(500, new { message = $"An error occurred while uploading the resume: {ex.Message}" });
        }
    }
}
