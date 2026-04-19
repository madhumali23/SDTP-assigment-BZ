using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class SupervisorInterest
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string SupervisorUserId { get; set; } = string.Empty;

    public int ProjectProposalId { get; set; }

    public DateTime ExpressedAtUtc { get; set; } = DateTime.UtcNow;
}
