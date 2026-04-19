namespace BlindMatchPAS.Web.Models;

public class SupervisorProposalActionViewModel
{
    public AnonymousProposalViewModel Proposal { get; set; } = new();
    public bool HasExpressedInterest { get; set; }
    public bool IsMatched { get; set; }
}
