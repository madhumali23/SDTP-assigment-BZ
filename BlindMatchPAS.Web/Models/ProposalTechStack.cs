using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class ProposalTechStack
{
    public int Id { get; set; }

    public int ProjectProposalId { get; set; }

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
