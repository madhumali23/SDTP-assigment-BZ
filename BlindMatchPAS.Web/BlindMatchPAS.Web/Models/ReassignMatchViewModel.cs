using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class ReassignMatchViewModel
{
    public int MatchId { get; set; }
    public int ProposalId { get; set; }
    public string ProposalTitle { get; set; } = string.Empty;

    [Required]
    [Display(Name = "New Supervisor")]
    public string NewSupervisorUserId { get; set; } = string.Empty;

    public List<SupervisorOptionViewModel> Supervisors { get; set; } = [];
}

public class SupervisorOptionViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
