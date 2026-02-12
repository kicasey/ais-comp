using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentStrategyAI.API.Models;

[Table("jobs")]
public class Job
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = "";

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = "";

    [MaxLength(128)]
    public string? Department { get; set; }

    [MaxLength(128)]
    public string? Location { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Optional source URL (e.g., original EY job posting link).
    /// </summary>
    [MaxLength(1024)]
    public string? SourceUrl { get; set; }
}

