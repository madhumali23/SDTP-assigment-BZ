namespace BlindMatchPAS.Web.Models;

public class AllocationOverviewItemViewModel
{
    public int MatchId { get; set; }
    public int ProposalId { get; set; }
    public string ProposalTitle { get; set; } = string.Empty;
    public string ResearchArea { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string SupervisorName { get; set; } = string.Empty;
    public string SupervisorEmail { get; set; } = string.Empty;
    public DateTime ConfirmedAtUtc { get; set; }
}
