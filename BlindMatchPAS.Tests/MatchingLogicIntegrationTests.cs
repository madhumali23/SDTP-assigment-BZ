using BlindMatchPAS.Web.Controllers;
using BlindMatchPAS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Tests;

public class MatchingLogicIntegrationTests
{
    [Fact]
    public async Task ExpressInterest_OnPendingProposal_TransitionsToUnderReview()
    {
        using var db = TestUtilities.CreateInMemoryDbContext();
        var userManager = TestUtilities.CreateUserManagerMock();

        var proposal = new ProjectProposal
        {
            Title = "Initial proposal",
            Abstract = "This abstract is intentionally long enough to satisfy model rules while exercising state transitions.",
            TechnicalStack = "C#, EF Core",
            ResearchArea = "Data Engineering",
            StudentUserId = "student-1",
            Status = ProposalStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ProjectProposals.Add(proposal);
        await db.SaveChangesAsync();

        var controller = new SupervisorController(db, userManager.Object);
        TestUtilities.AttachUser(controller, "supervisor-1", "Supervisor");

        var result = await controller.ExpressInterest(proposal.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Proposal", redirect.ActionName);

        var saved = await db.ProjectProposals.FindAsync(proposal.Id);
        Assert.NotNull(saved);
        Assert.Equal(ProposalStatus.UnderReview, saved!.Status);
        Assert.True(await db.SupervisorInterests.AnyAsync(i => i.ProjectProposalId == proposal.Id && i.SupervisorUserId == "supervisor-1"));
    }

    [Fact]
    public async Task ConfirmMatch_WithInterest_PersistsAssignment_AndRevealsTriggerState()
    {
        var (db, connection) = TestUtilities.CreateSqliteDbContext();
        try
        {
            var userManager = TestUtilities.CreateUserManagerMock();

            var proposal = new ProjectProposal
            {
                Title = "Transaction-safe matching",
                Abstract = "This abstract is intentionally long enough to satisfy model rules and verify confirm-match transaction behavior.",
                TechnicalStack = "ASP.NET Core, EF Core",
                ResearchArea = "Software Engineering",
                StudentUserId = "student-1",
                Status = ProposalStatus.UnderReview,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.ProjectProposals.Add(proposal);
            await db.SaveChangesAsync();

            db.SupervisorInterests.Add(new SupervisorInterest
            {
                SupervisorUserId = "supervisor-1",
                ProjectProposalId = proposal.Id,
                ExpressedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var controller = new SupervisorController(db, userManager.Object);
            TestUtilities.AttachUser(controller, "supervisor-1", "Supervisor");

            var result = await controller.ConfirmMatch(proposal.Id);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MatchDetails", redirect.ActionName);

            var updated = await db.ProjectProposals.FindAsync(proposal.Id);
            Assert.NotNull(updated);
            Assert.Equal(ProposalStatus.Matched, updated!.Status);

            var match = await db.MatchAssignments.FirstOrDefaultAsync(m => m.ProjectProposalId == proposal.Id);
            Assert.NotNull(match);
            Assert.Equal("student-1", match!.StudentUserId);
            Assert.Equal("supervisor-1", match.SupervisorUserId);

            Assert.True(await db.AuditLogs.AnyAsync(a => a.ActionType == "Supervisor.ConfirmMatch" && a.EntityId == proposal.Id.ToString()));
        }
        finally
        {
            await db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
