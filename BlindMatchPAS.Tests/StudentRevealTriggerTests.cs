using BlindMatchPAS.Web.Controllers;
using BlindMatchPAS.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BlindMatchPAS.Tests;

public class StudentRevealTriggerTests
{
    [Fact]
    public async Task Details_WhenMatched_RevealsStudentAndSupervisorIdentity()
    {
        using var db = TestUtilities.CreateInMemoryDbContext();
        var userManager = TestUtilities.CreateUserManagerMock();

        var proposal = new ProjectProposal
        {
            Id = 10,
            Title = "Blind review system",
            Abstract = "This abstract is intentionally long enough to satisfy the model constraints for details view testing.",
            TechnicalStack = "ASP.NET Core, EF Core",
            ResearchArea = "Distributed Systems",
            StudentUserId = "student-1",
            Status = ProposalStatus.Matched,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.ProjectProposals.Add(proposal);
        db.MatchAssignments.Add(new MatchAssignment
        {
            ProjectProposalId = proposal.Id,
            StudentUserId = "student-1",
            SupervisorUserId = "supervisor-1",
            ConfirmedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        userManager
            .Setup(x => x.FindByIdAsync("student-1"))
            .ReturnsAsync(new IdentityUser { Id = "student-1", UserName = "Student One", Email = "student1@test.local" });

        userManager
            .Setup(x => x.FindByIdAsync("supervisor-1"))
            .ReturnsAsync(new IdentityUser { Id = "supervisor-1", UserName = "Supervisor One", Email = "supervisor1@test.local" });

        var controller = new StudentController(db, userManager.Object);
        TestUtilities.AttachUser(controller, "student-1", "Student");

        var result = await controller.Details(proposal.Id);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MatchDetailsViewModel>(view.Model);
        Assert.Equal("Student One", model.StudentName);
        Assert.Equal("student1@test.local", model.StudentEmail);
        Assert.Equal("Supervisor One", model.SupervisorName);
        Assert.Equal("supervisor1@test.local", model.SupervisorEmail);
    }

    [Fact]
    public async Task Details_WhenUnmatched_DoesNotRevealIdentity()
    {
        using var db = TestUtilities.CreateInMemoryDbContext();
        var userManager = TestUtilities.CreateUserManagerMock();

        var proposal = new ProjectProposal
        {
            Id = 11,
            Title = "Anonymous proposals",
            Abstract = "This abstract is intentionally long enough to satisfy the model constraints for unmatched details testing.",
            TechnicalStack = "ASP.NET Core, SQL Server",
            ResearchArea = "Cybersecurity",
            StudentUserId = "student-1",
            Status = ProposalStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.ProjectProposals.Add(proposal);
        await db.SaveChangesAsync();

        var controller = new StudentController(db, userManager.Object);
        TestUtilities.AttachUser(controller, "student-1", "Student");

        var result = await controller.Details(proposal.Id);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MatchDetailsViewModel>(view.Model);
        Assert.Null(model.StudentName);
        Assert.Null(model.StudentEmail);
        Assert.Null(model.SupervisorName);
        Assert.Null(model.SupervisorEmail);
    }
}
