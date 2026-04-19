using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class MatchAssignment
{
    public int Id { get; set; }

    public int ProjectProposalId { get; set; }

    [Required]
    [MaxLength(450)]
    public string StudentUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(450)]
    public string SupervisorUserId { get; set; } = string.Empty;

    public DateTime ConfirmedAtUtc { get; set; } = DateTime.UtcNow;
}
