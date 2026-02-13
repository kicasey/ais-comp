using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentStrategyAI.API.Data;
using TalentStrategyAI.API.Models;

namespace TalentStrategyAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResumeController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly AppDbContext _db;
    private readonly ILogger<ResumeController> _logger;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx" };

    public ResumeController(
        IWebHostEnvironment environment,
        AppDbContext db,
        ILogger<ResumeController> logger)
    {
        _environment = environment;
        _db = db;
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null) return Unauthorized();
        if (user.Role != "employee")
            return Ok(new { isEmployee = false, message = "Resume is for employee profiles only." });

        var profile = await _db.EmployeeProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new EmployeeProfile
            {
                UserId = userId,
                Name = user.DisplayName,
                Position = null,
                Department = null
            };
            _db.EmployeeProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            isEmployee = true,
            displayName = user.DisplayName,
            email = user.Email,
            name = profile.Name,
            position = profile.Position,
            department = profile.Department,
            hasResume = !string.IsNullOrEmpty(profile.ResumeFilePath),
            resumeFileName = profile.ResumeFileName,
            resumeUploadedAt = profile.ResumeUploadedAt
        });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadResume([FromForm] IFormFile resume)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null) return Unauthorized();
        if (user.Role != "employee")
            return Forbid();

        if (resume == null || resume.Length == 0)
            return BadRequest(new { message = "No file uploaded." });
        if (resume.Length > MaxFileSize)
            return BadRequest(new { message = "File size exceeds 10MB limit." });

        var fileExtension = Path.GetExtension(resume.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(fileExtension))
            return BadRequest(new { message = "Invalid file type. Only PDF, DOC, and DOCX are allowed." });

        var profile = await _db.EmployeeProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new EmployeeProfile { UserId = userId, Name = user.DisplayName };
            _db.EmployeeProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }

        var uploadsPath = Path.Combine(_environment.ContentRootPath, "Uploads", "Resumes");
        if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

        if (!string.IsNullOrEmpty(profile.ResumeFilePath))
        {
            var oldPath = Path.Combine(_environment.ContentRootPath, "Uploads", profile.ResumeFilePath.Replace('\\', '/').TrimStart('/'));
            if (System.IO.File.Exists(oldPath))
            {
                try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
            }
        }

        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var relativePath = $"Resumes/{fileName}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await resume.CopyToAsync(stream);

        profile.ResumeFileName = resume.FileName;
        profile.ResumeFilePath = relativePath;
        profile.ResumeUploadedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Resume uploaded for user {UserId}, profile {ProfileId}", userId, profile.Id);

        return Ok(new
        {
            message = "Resume saved to your profile.",
            fileName = resume.FileName,
            resumeUploadedAt = profile.ResumeUploadedAt
        });
    }

    private int? GetCurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub) || !int.TryParse(sub, out var id)) return null;
        return id;
    }
}
