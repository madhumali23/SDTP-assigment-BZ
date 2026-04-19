using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class ResearchAreaInputModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}
