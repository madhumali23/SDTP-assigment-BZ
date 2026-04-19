using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class ResearchArea
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
