using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class StudentProposalInputModel
{
    [Required]
    [StringLength(150, MinimumLength = 6)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 30)]
    public string Abstract { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Technical Stack")]
    [StringLength(300, MinimumLength = 3)]
    public string TechnicalStack { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Research Area")]
    [StringLength(120, MinimumLength = 2)]
    public string ResearchArea { get; set; } = string.Empty;
}
