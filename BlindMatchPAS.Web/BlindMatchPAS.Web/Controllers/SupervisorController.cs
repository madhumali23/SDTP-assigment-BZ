using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlindMatchPAS.Web.Data;
using BlindMatchPAS.Web.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace BlindMatchPAS.Web.Controllers;

[Authorize(Roles = "Supervisor")]
public class SupervisorController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public SupervisorController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? researchArea, bool onlyMyAreas = true)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var expertiseAreas = await _db.SupervisorExpertise
            .Where(e => e.SupervisorUserId == userId)
            .Select(e => e.ResearchArea)
            .OrderBy(e => e)
            .ToListAsync();

        var availableResearchAreas = await _db.ProjectProposals
            .Where(p => p.Status == ProposalStatus.Pending || p.Status == ProposalStatus.UnderReview)
            .Select(p => p.ResearchArea)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        var query = _db.ProjectProposals
            .AsNoTracking()
            .Where(p => p.Status == ProposalStatus.Pending || p.Status == ProposalStatus.UnderReview);

        if (onlyMyAreas && expertiseAreas.Count > 0)
        {
            query = query.Where(p => expertiseAreas.Contains(p.ResearchArea));
        }

        if (!string.IsNullOrWhiteSpace(researchArea))
        {
            query = query.Where(p => p.ResearchArea == researchArea);
        }

        var proposals = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new AnonymousProposalViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Abstract = p.Abstract,
                TechnicalStack = p.TechnicalStack,
                ResearchArea = p.ResearchArea,
                Status = p.Status,
                CreatedAtUtc = p.CreatedAtUtc
            })
            .ToListAsync();

        var model = new SupervisorDashboardViewModel
        {
            Proposals = proposals,
            ExpertiseAreas = expertiseAreas,
            AvailableResearchAreas = availableResearchAreas,
            SelectedResearchArea = researchArea,
            OnlyMyAreas = onlyMyAreas
        };

        return View(model);
    }

    public async Task<IActionResult> Expertise()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var expertise = await _db.SupervisorExpertise
            .Where(e => e.SupervisorUserId == userId)
            .OrderBy(e => e.ResearchArea)
            .ToListAsync();

        return View(expertise);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExpertise(string researchArea)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var normalized = (researchArea ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length < 2 || normalized.Length > 120)
        {
            TempData["ErrorMessage"] = "Research area must be between 2 and 120 characters.";
            return RedirectToAction(nameof(Expertise));
        }

        var exists = await _db.SupervisorExpertise.AnyAsync(e =>
            e.SupervisorUserId == userId && e.ResearchArea.ToLower() == normalized.ToLower());

        if (exists)
        {
            TempData["ErrorMessage"] = "This expertise tag already exists.";
            return RedirectToAction(nameof(Expertise));
        }

        _db.SupervisorExpertise.Add(new SupervisorExpertise
        {
            SupervisorUserId = userId,
            ResearchArea = normalized,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Expertise added.";
        return RedirectToAction(nameof(Expertise));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveExpertise(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var expertise = await _db.SupervisorExpertise
            .FirstOrDefaultAsync(e => e.Id == id && e.SupervisorUserId == userId);

        if (expertise is null)
        {
            return NotFound();
        }

        _db.SupervisorExpertise.Remove(expertise);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Expertise removed.";
        return RedirectToAction(nameof(Expertise));
    }

    public async Task<IActionResult> Proposal(int id)
    {
        var proposal = await _db.ProjectProposals
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.Status != ProposalStatus.Withdrawn);

        if (proposal is null)
        {
            return NotFound();
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var hasExpressedInterest = await _db.SupervisorInterests.AnyAsync(i => i.SupervisorUserId == userId && i.ProjectProposalId == proposal.Id);
        var isMatched = await _db.MatchAssignments.AnyAsync(m => m.ProjectProposalId == proposal.Id);

        var model = new SupervisorProposalActionViewModel
        {
            Proposal = new AnonymousProposalViewModel
            {
                Id = proposal.Id,
                Title = proposal.Title,
                Abstract = proposal.Abstract,
                TechnicalStack = proposal.TechnicalStack,
                ResearchArea = proposal.ResearchArea,
                Status = proposal.Status,
                CreatedAtUtc = proposal.CreatedAtUtc
            },
            HasExpressedInterest = hasExpressedInterest,
            IsMatched = isMatched
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpressInterest(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var proposal = await _db.ProjectProposals.FirstOrDefaultAsync(p => p.Id == id);
        if (proposal is null || proposal.Status == ProposalStatus.Withdrawn)
        {
            return NotFound();
        }

        var alreadyInterested = await _db.SupervisorInterests.AnyAsync(i => i.SupervisorUserId == userId && i.ProjectProposalId == id);
        if (!alreadyInterested)
        {
            _db.SupervisorInterests.Add(new SupervisorInterest
            {
                SupervisorUserId = userId,
                ProjectProposalId = id,
                ExpressedAtUtc = DateTime.UtcNow
            });
        }

        if (proposal.Status == ProposalStatus.Pending)
        {
            proposal.Status = ProposalStatus.UnderReview;
            proposal.UpdatedAtUtc = DateTime.UtcNow;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            ActionType = "Supervisor.ExpressInterest",
            EntityName = nameof(ProjectProposal),
            EntityId = proposal.Id.ToString(),
            Details = $"Expressed interest in '{proposal.Title}'.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Interest recorded. The proposal is now under review.";
        return RedirectToAction(nameof(Proposal), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmMatch(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var proposal = await _db.ProjectProposals.FirstOrDefaultAsync(p => p.Id == id);
        if (proposal is null)
        {
            return NotFound();
        }

        if (proposal.Status == ProposalStatus.Withdrawn)
        {
            TempData["ErrorMessage"] = "Withdrawn proposals cannot be matched.";
            return RedirectToAction(nameof(Proposal), new { id });
        }

        var hasInterest = await _db.SupervisorInterests.AnyAsync(i => i.SupervisorUserId == userId && i.ProjectProposalId == id);
        if (!hasInterest)
        {
            TempData["ErrorMessage"] = "Express interest before confirming the match.";
            return RedirectToAction(nameof(Proposal), new { id });
        }

        var existingMatch = await _db.MatchAssignments.AnyAsync(m => m.ProjectProposalId == id);
        if (existingMatch || proposal.Status == ProposalStatus.Matched)
        {
            TempData["ErrorMessage"] = "This proposal is already matched.";
            return RedirectToAction(nameof(Proposal), new { id });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        proposal.Status = ProposalStatus.Matched;
        proposal.UpdatedAtUtc = DateTime.UtcNow;

        _db.MatchAssignments.Add(new MatchAssignment
        {
            ProjectProposalId = proposal.Id,
            StudentUserId = proposal.StudentUserId,
            SupervisorUserId = userId,
            ConfirmedAtUtc = DateTime.UtcNow
        });

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            ActionType = "Supervisor.ConfirmMatch",
            EntityName = nameof(ProjectProposal),
            EntityId = proposal.Id.ToString(),
            Details = $"Confirmed match for '{proposal.Title}'.",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = "Match confirmed and identities revealed to both parties.";
        return RedirectToAction(nameof(MatchDetails), new { id });
    }

    public async Task<IActionResult> MatchDetails(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var match = await _db.MatchAssignments.FirstOrDefaultAsync(m => m.ProjectProposalId == id);
        if (match is null)
        {
            return NotFound();
        }

        var proposal = await _db.ProjectProposals.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (proposal is null)
        {
            return NotFound();
        }

        var student = await _userManager.FindByIdAsync(match.StudentUserId);
        var supervisor = await _userManager.FindByIdAsync(match.SupervisorUserId);

        var model = new MatchDetailsViewModel
        {
            ProposalId = proposal.Id,
            ProposalTitle = proposal.Title,
            ProposalAbstract = proposal.Abstract,
            ResearchArea = proposal.ResearchArea,
            TechnicalStack = proposal.TechnicalStack,
            Status = proposal.Status,
            CreatedAtUtc = proposal.CreatedAtUtc,
            StudentName = student?.UserName,
            StudentEmail = student?.Email,
            SupervisorName = supervisor?.UserName,
            SupervisorEmail = supervisor?.Email,
            ConfirmedAtUtc = match.ConfirmedAtUtc
        };

        return View(model);
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
