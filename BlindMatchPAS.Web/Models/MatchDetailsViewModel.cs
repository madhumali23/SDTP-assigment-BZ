namespace BlindMatchPAS.Web.Models;

public class MatchDetailsViewModel
{
    public int ProposalId { get; set; }
    public string ProposalTitle { get; set; } = string.Empty;
    public string ProposalAbstract { get; set; } = string.Empty;
    public string ResearchArea { get; set; } = string.Empty;
    public string TechnicalStack { get; set; } = string.Empty;
    public ProposalStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? StudentName { get; set; }
    public string? StudentEmail { get; set; }
    public string? SupervisorName { get; set; }
    public string? SupervisorEmail { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
}
