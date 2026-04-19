using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class SupervisorProfile
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? DisplayName { get; set; }

    [MaxLength(1000)]
    public string? Bio { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
