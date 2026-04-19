namespace BlindMatchPAS.Web.Models;

public class SupervisorDashboardViewModel
{
    public List<AnonymousProposalViewModel> Proposals { get; set; } = [];
    public List<string> ExpertiseAreas { get; set; } = [];
    public List<string> AvailableResearchAreas { get; set; } = [];
    public string? SelectedResearchArea { get; set; }
    public bool OnlyMyAreas { get; set; }
}
