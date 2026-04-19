using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class ProjectProposal
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Abstract { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string TechnicalStack { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string ResearchArea { get; set; } = string.Empty;

    [Required]
    [MaxLength(450)]
    public string StudentUserId { get; set; } = string.Empty;

    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
