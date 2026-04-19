using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlindMatchPAS.Web.Controllers;

[Authorize(Roles = "SystemAdministrator")]
public class SystemAdminController : Controller
{
    private static readonly string[] AssignableRoles = ["Student", "Supervisor", "ModuleLeader", "SystemAdministrator"];

    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _db;

    public SystemAdminController(UserManager<IdentityUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.ToList();
        var list = new List<SystemUserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            list.Add(new SystemUserListItemViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "-",
                UserName = user.UserName ?? "-",
                Role = roles.FirstOrDefault() ?? "(No Role)"
            });
        }

        var model = new SystemAdminIndexViewModel
        {
            Users = list.OrderBy(u => u.Email).ToList(),
            AvailableRoles = AssignableRoles.ToList(),
            NewUser = new CreateUserInputModel()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserInputModel input)
    {
        if (!ModelState.IsValid || !AssignableRoles.Contains(input.Role))
        {
            TempData["ErrorMessage"] = "Invalid user details.";
            return RedirectToAction(nameof(Index));
        }

        var email = input.Email.Trim();
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            TempData["ErrorMessage"] = "A user with this email already exists.";
            return RedirectToAction(nameof(Index));
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, input.Password);
        if (!createResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", createResult.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        await _userManager.AddToRoleAsync(user, input.Role);

        await EnsureProfileAsync(user.Id, input.Role);
        await WriteAuditAsync("SystemAdmin.CreateUser", nameof(IdentityUser), user.Id, $"Created user '{user.Email}' with role '{input.Role}'.");

        TempData["SuccessMessage"] = "User created and role assigned.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRole(string userId, string role)
    {
        if (string.IsNullOrWhiteSpace(userId) || !AssignableRoles.Contains(role))
        {
            TempData["ErrorMessage"] = "Invalid role update request.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }
        await _userManager.AddToRoleAsync(user, role);

        await EnsureProfileAsync(user.Id, role);
        await WriteAuditAsync("SystemAdmin.UpdateUserRole", nameof(IdentityUser), user.Id, $"Updated role to '{role}' for user '{user.Email}'.");

        TempData["SuccessMessage"] = "User role updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var studentProfile = await _db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (studentProfile is not null)
        {
            _db.StudentProfiles.Remove(studentProfile);
        }

        var supervisorProfile = await _db.SupervisorProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (supervisorProfile is not null)
        {
            _db.SupervisorProfiles.Remove(supervisorProfile);
        }

        var logs = await _db.AuditLogs.Where(a => a.ActorUserId == user.Id).ToListAsync();
        if (logs.Count > 0)
        {
            _db.AuditLogs.RemoveRange(logs);
        }

        await _db.SaveChangesAsync();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        await WriteAuditAsync("SystemAdmin.DeleteUser", nameof(IdentityUser), userId, $"Deleted user '{user.Email}'.");

        TempData["SuccessMessage"] = "User deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task EnsureProfileAsync(string userId, string role)
    {
        if (role == "Student")
        {
            var hasStudentProfile = await _db.StudentProfiles.AnyAsync(p => p.UserId == userId);
            if (!hasStudentProfile)
            {
                _db.StudentProfiles.Add(new StudentProfile
                {
                    UserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        if (role == "Supervisor")
        {
            var hasSupervisorProfile = await _db.SupervisorProfiles.AnyAsync(p => p.UserId == userId);
            if (!hasSupervisorProfile)
            {
                _db.SupervisorProfiles.Add(new SupervisorProfile
                {
                    UserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task WriteAuditAsync(string actionType, string entityName, string? entityId, string? details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            ActionType = actionType,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
