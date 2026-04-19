using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlindMatchPAS.Web.Data;
using BlindMatchPAS.Web.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace BlindMatchPAS.Web.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public StudentController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var proposals = await _db.ProjectProposals
            .Where(p => p.StudentUserId == userId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync();

        return View(proposals);
    }

    public async Task<IActionResult> Details(int id)
    {
        var proposal = await GetOwnedProposalAsync(id);
        if (proposal is null)
        {
            return NotFound();
        }

        var match = await _db.MatchAssignments.FirstOrDefaultAsync(m => m.ProjectProposalId == proposal.Id);
        var model = new MatchDetailsViewModel
        {
            ProposalId = proposal.Id,
            ProposalTitle = proposal.Title,
            ProposalAbstract = proposal.Abstract,
            ResearchArea = proposal.ResearchArea,
            TechnicalStack = proposal.TechnicalStack,
            Status = proposal.Status,
            CreatedAtUtc = proposal.CreatedAtUtc,
            ConfirmedAtUtc = match?.ConfirmedAtUtc
        };

        if (match is not null)
        {
            var student = await _userManager.FindByIdAsync(match.StudentUserId);
            var supervisor = await _userManager.FindByIdAsync(match.SupervisorUserId);
            model.StudentName = student?.UserName;
            model.StudentEmail = student?.Email;
            model.SupervisorName = supervisor?.UserName;
            model.SupervisorEmail = supervisor?.Email;
        }

        return View(model);
    }

    public IActionResult Create()
    {
        return View(new StudentProposalInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentProposalInputModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var proposal = new ProjectProposal
        {
            Title = model.Title.Trim(),
            Abstract = model.Abstract.Trim(),
            TechnicalStack = model.TechnicalStack.Trim(),
            ResearchArea = model.ResearchArea.Trim(),
            StudentUserId = userId,
            Status = ProposalStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ProjectProposals.Add(proposal);
        await _db.SaveChangesAsync();

        await ReplaceProposalTechStacksAsync(proposal.Id, model.TechnicalStack);
        await WriteAuditAsync(userId, "Student.CreateProposal", nameof(ProjectProposal), proposal.Id.ToString(), $"Created proposal '{proposal.Title}'.");

        TempData["SuccessMessage"] = "Proposal submitted successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var proposal = await GetOwnedProposalAsync(id);
        if (proposal is null)
        {
            return NotFound();
        }

        if (!CanEditOrWithdraw(proposal.Status))
        {
            TempData["ErrorMessage"] = "Matched or withdrawn proposals cannot be edited.";
            return RedirectToAction(nameof(Index));
        }

        var model = new StudentProposalInputModel
        {
            Title = proposal.Title,
            Abstract = proposal.Abstract,
            TechnicalStack = proposal.TechnicalStack,
            ResearchArea = proposal.ResearchArea
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentProposalInputModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var proposal = await GetOwnedProposalAsync(id);
        if (proposal is null)
        {
            return NotFound();
        }

        if (!CanEditOrWithdraw(proposal.Status))
        {
            TempData["ErrorMessage"] = "Matched or withdrawn proposals cannot be edited.";
            return RedirectToAction(nameof(Index));
        }

        proposal.Title = model.Title.Trim();
        proposal.Abstract = model.Abstract.Trim();
        proposal.TechnicalStack = model.TechnicalStack.Trim();
        proposal.ResearchArea = model.ResearchArea.Trim();
        proposal.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await ReplaceProposalTechStacksAsync(proposal.Id, model.TechnicalStack);
        await WriteAuditAsync(GetCurrentUserId(), "Student.EditProposal", nameof(ProjectProposal), proposal.Id.ToString(), $"Updated proposal '{proposal.Title}'.");

        TempData["SuccessMessage"] = "Proposal updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id)
    {
        var proposal = await GetOwnedProposalAsync(id);
        if (proposal is null)
        {
            return NotFound();
        }

        if (!CanEditOrWithdraw(proposal.Status))
        {
            TempData["ErrorMessage"] = "Only pending or under review proposals can be withdrawn.";
            return RedirectToAction(nameof(Index));
        }

        proposal.Status = ProposalStatus.Withdrawn;
        proposal.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await WriteAuditAsync(GetCurrentUserId(), "Student.WithdrawProposal", nameof(ProjectProposal), proposal.Id.ToString(), $"Withdrew proposal '{proposal.Title}'.");

        TempData["SuccessMessage"] = "Proposal withdrawn.";
        return RedirectToAction(nameof(Index));
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task<ProjectProposal?> GetOwnedProposalAsync(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return null;
        }

        return await _db.ProjectProposals
            .FirstOrDefaultAsync(p => p.Id == id && p.StudentUserId == userId);
    }

    private static bool CanEditOrWithdraw(ProposalStatus status)
    {
        return status is ProposalStatus.Pending or ProposalStatus.UnderReview;
    }

    private async Task ReplaceProposalTechStacksAsync(int proposalId, string rawTechStack)
    {
        var existing = await _db.ProposalTechStacks
            .Where(t => t.ProjectProposalId == proposalId)
            .ToListAsync();

        if (existing.Count > 0)
        {
            _db.ProposalTechStacks.RemoveRange(existing);
        }

        var stacks = ParseTechStack(rawTechStack)
            .Select(name => new ProposalTechStack
            {
                ProjectProposalId = proposalId,
                Name = name,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (stacks.Count > 0)
        {
            _db.ProposalTechStacks.AddRange(stacks);
        }

        await _db.SaveChangesAsync();
    }

    private static IEnumerable<string> ParseTechStack(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        return Regex.Split(input, @"[,;\n\r]+")
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20);
    }

    private async Task WriteAuditAsync(string? actorUserId, string actionType, string entityName, string? entityId, string? details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            ActionType = actionType,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
