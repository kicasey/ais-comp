namespace TalentStrategyAI.API.Models;

public class EmployeeProfile
{
    public int Id { get; set; }
    public int? UserId { get; set; } // Link to User when created via "Create profile"
    public string? Name { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? ResumeFileName { get; set; }
    public string? ResumeFilePath { get; set; }
    public DateTime? ResumeUploadedAt { get; set; }
}

