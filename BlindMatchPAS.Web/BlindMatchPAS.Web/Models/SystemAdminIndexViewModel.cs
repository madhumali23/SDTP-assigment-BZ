using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models;

public class SystemAdminIndexViewModel
{
    public List<SystemUserListItemViewModel> Users { get; set; } = [];
    public List<string> AvailableRoles { get; set; } = [];
    public CreateUserInputModel NewUser { get; set; } = new();
}

public class SystemUserListItemViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CreateUserInputModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}
