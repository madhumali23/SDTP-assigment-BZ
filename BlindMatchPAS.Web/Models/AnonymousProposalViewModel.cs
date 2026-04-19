namespace BlindMatchPAS.Web.Models;

public class AnonymousProposalViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
    public string TechnicalStack { get; set; } = string.Empty;
    public string ResearchArea { get; set; } = string.Empty;
    public ProposalStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
