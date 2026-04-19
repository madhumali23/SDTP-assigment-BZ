using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BlindMatchPAS.Web.Data;
using BlindMatchPAS.Web.Models;
using System.Security.Claims;

namespace BlindMatchPAS.Web.Controllers;

[Authorize(Roles = "ModuleLeader")]
public class ModuleLeaderController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public ModuleLeaderController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["AreasCount"] = await _db.ResearchAreas.CountAsync();
        ViewData["MatchCount"] = await _db.MatchAssignments.CountAsync();
        ViewData["PendingCount"] = await _db.ProjectProposals.CountAsync(p => p.Status == ProposalStatus.Pending || p.Status == ProposalStatus.UnderReview);
        return View();
    }

    public async Task<IActionResult> ResearchAreas()
    {
        var model = new ResearchAreaManagementViewModel
        {
            Areas = await _db.ResearchAreas.OrderBy(r => r.Name).ToListAsync(),
            NewArea = new ResearchAreaInputModel()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateResearchArea(ResearchAreaInputModel model)
    {
        if (!ModelState.IsValid)
        {
            var vm = new ResearchAreaManagementViewModel
            {
                Areas = await _db.ResearchAreas.OrderBy(r => r.Name).ToListAsync(),
                NewArea = model
            };
            return View("ResearchAreas", vm);
        }

        var name = model.Name.Trim();
        var exists = await _db.ResearchAreas.AnyAsync(r => r.Name.ToLower() == name.ToLower());
        if (exists)
        {
            TempData["ErrorMessage"] = "Research area already exists.";
            return RedirectToAction(nameof(ResearchAreas));
        }

        _db.ResearchAreas.Add(new ResearchArea
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = GetCurrentUserId(),
            ActionType = "ModuleLeader.CreateResearchArea",
            EntityName = nameof(ResearchArea),
            EntityId = name,
            Details = $"Created research area '{name}'.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Research area created.";
        return RedirectToAction(nameof(ResearchAreas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateResearchArea(ResearchAreaInputModel model)
    {
        if (!ModelState.IsValid || !model.Id.HasValue)
        {
            TempData["ErrorMessage"] = "Invalid research area update.";
            return RedirectToAction(nameof(ResearchAreas));
        }

        var area = await _db.ResearchAreas.FirstOrDefaultAsync(r => r.Id == model.Id.Value);
        if (area is null)
        {
            return NotFound();
        }

        var name = model.Name.Trim();
        var duplicate = await _db.ResearchAreas.AnyAsync(r => r.Id != area.Id && r.Name.ToLower() == name.ToLower());
        if (duplicate)
        {
            TempData["ErrorMessage"] = "Another research area already has this name.";
            return RedirectToAction(nameof(ResearchAreas));
        }

        area.Name = name;
        area.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = GetCurrentUserId(),
            ActionType = "ModuleLeader.UpdateResearchArea",
            EntityName = nameof(ResearchArea),
            EntityId = area.Id.ToString(),
            Details = $"Updated research area '{area.Name}'.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Research area updated.";
        return RedirectToAction(nameof(ResearchAreas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResearchArea(int id)
    {
        var area = await _db.ResearchAreas.FirstOrDefaultAsync(r => r.Id == id);
        if (area is null)
        {
            return NotFound();
        }

        _db.ResearchAreas.Remove(area);

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = GetCurrentUserId(),
            ActionType = "ModuleLeader.DeleteResearchArea",
            EntityName = nameof(ResearchArea),
            EntityId = area.Id.ToString(),
            Details = $"Deleted research area '{area.Name}'.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Research area deleted.";
        return RedirectToAction(nameof(ResearchAreas));
    }

    public async Task<IActionResult> Allocations()
    {
        var matches = await _db.MatchAssignments
            .OrderByDescending(m => m.ConfirmedAtUtc)
            .ToListAsync();

        var proposalMap = await _db.ProjectProposals
            .Where(p => matches.Select(m => m.ProjectProposalId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var userIds = matches.Select(m => m.StudentUserId)
            .Concat(matches.Select(m => m.SupervisorUserId))
            .Distinct()
            .ToList();

        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        var model = matches.Select(match =>
        {
            var proposal = proposalMap.GetValueOrDefault(match.ProjectProposalId);
            var student = users.GetValueOrDefault(match.StudentUserId);
            var supervisor = users.GetValueOrDefault(match.SupervisorUserId);

            return new AllocationOverviewItemViewModel
            {
                MatchId = match.Id,
                ProposalId = match.ProjectProposalId,
                ProposalTitle = proposal?.Title ?? "(Missing Proposal)",
                ResearchArea = proposal?.ResearchArea ?? "-",
                StudentName = student?.UserName ?? "-",
                StudentEmail = student?.Email ?? "-",
                SupervisorName = supervisor?.UserName ?? "-",
                SupervisorEmail = supervisor?.Email ?? "-",
                ConfirmedAtUtc = match.ConfirmedAtUtc
            };
        }).ToList();

        return View(model);
    }

    public async Task<IActionResult> Reassign(int matchId)
    {
        var match = await _db.MatchAssignments.FirstOrDefaultAsync(m => m.Id == matchId);
        if (match is null)
        {
            return NotFound();
        }

        var proposal = await _db.ProjectProposals.FirstOrDefaultAsync(p => p.Id == match.ProjectProposalId);
        if (proposal is null)
        {
            return NotFound();
        }

        var supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");
        var supervisorOptions = supervisors
            .Select(s => new SupervisorOptionViewModel
            {
                UserId = s.Id,
                DisplayName = string.IsNullOrWhiteSpace(s.Email) ? (s.UserName ?? s.Id) : $"{s.UserName} ({s.Email})"
            })
            .OrderBy(s => s.DisplayName)
            .ToList();

        var model = new ReassignMatchViewModel
        {
            MatchId = match.Id,
            ProposalId = proposal.Id,
            ProposalTitle = proposal.Title,
            NewSupervisorUserId = match.SupervisorUserId,
            Supervisors = supervisorOptions
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reassign(ReassignMatchViewModel model)
    {
        var match = await _db.MatchAssignments.FirstOrDefaultAsync(m => m.Id == model.MatchId);
        if (match is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(model.NewSupervisorUserId))
        {
            ModelState.AddModelError(nameof(model.NewSupervisorUserId), "Select a supervisor.");
        }

        var newSupervisor = string.IsNullOrWhiteSpace(model.NewSupervisorUserId)
            ? null
            : await _userManager.FindByIdAsync(model.NewSupervisorUserId);

        if (newSupervisor is null || !await _userManager.IsInRoleAsync(newSupervisor, "Supervisor"))
        {
            ModelState.AddModelError(nameof(model.NewSupervisorUserId), "Selected account is not a supervisor.");
        }

        if (!ModelState.IsValid)
        {
            var supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");
            model.Supervisors = supervisors
                .Select(s => new SupervisorOptionViewModel
                {
                    UserId = s.Id,
                    DisplayName = string.IsNullOrWhiteSpace(s.Email) ? (s.UserName ?? s.Id) : $"{s.UserName} ({s.Email})"
                })
                .OrderBy(s => s.DisplayName)
                .ToList();
            return View(model);
        }

        match.SupervisorUserId = model.NewSupervisorUserId;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = GetCurrentUserId(),
            ActionType = "ModuleLeader.ReassignMatch",
            EntityName = nameof(MatchAssignment),
            EntityId = match.Id.ToString(),
            Details = $"Reassigned match {match.Id} to supervisor '{model.NewSupervisorUserId}'.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Match reassigned successfully.";
        return RedirectToAction(nameof(Allocations));
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
