namespace BlindMatchPAS.Web.Models;

public class ResearchAreaManagementViewModel
{
    public List<ResearchArea> Areas { get; set; } = [];
    public ResearchAreaInputModel NewArea { get; set; } = new();
}
