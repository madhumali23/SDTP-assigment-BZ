using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class SupervisorExpertise
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string SupervisorUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string ResearchArea { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
