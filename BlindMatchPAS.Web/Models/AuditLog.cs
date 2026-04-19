using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class AuditLog
{
    public int Id { get; set; }

    [MaxLength(450)]
    public string? ActorUserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ActionType { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? EntityId { get; set; }

    [MaxLength(1000)]
    public string? Details { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
