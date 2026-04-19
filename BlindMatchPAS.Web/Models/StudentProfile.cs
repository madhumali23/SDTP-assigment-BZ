using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class StudentProfile
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? FullName { get; set; }

    [MaxLength(120)]
    public string? Department { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
