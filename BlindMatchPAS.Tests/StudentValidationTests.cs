using BlindMatchPAS.Web.Controllers;
using BlindMatchPAS.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BlindMatchPAS.Tests;

public class StudentValidationTests
{
    [Fact]
    public async Task Create_WithInvalidModel_ReturnsView_AndDoesNotPersist()
    {
        using var db = TestUtilities.CreateInMemoryDbContext();
        var userManager = TestUtilities.CreateUserManagerMock();
        var controller = new StudentController(db, userManager.Object);

        controller.ModelState.AddModelError("Title", "Title is required.");
        var model = new StudentProposalInputModel();

        var result = await controller.Create(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.Empty(db.ProjectProposals);
    }

    [Fact]
    public async Task Edit_WhenProposalMatched_BlocksUpdate()
    {
        using var db = TestUtilities.CreateInMemoryDbContext();
        var userManager = TestUtilities.CreateUserManagerMock();

        var proposal = new ProjectProposal
        {
            Title = "Original title",
            Abstract = "This abstract is intentionally long enough to satisfy validation requirements for this test case.",
            TechnicalStack = "C#, ASP.NET Core",
            ResearchArea = "AI",
            StudentUserId = "student-1",
            Status = ProposalStatus.Matched,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ProjectProposals.Add(proposal);
        await db.SaveChangesAsync();

        var controller = new StudentController(db, userManager.Object);
        TestUtilities.AttachUser(controller, "student-1", "Student");

        var model = TestUtilities.BuildValidStudentProposalModel();
        model.Title = "Updated title should not be saved";

        var result = await controller.Edit(proposal.Id, model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var saved = await db.ProjectProposals.FindAsync(proposal.Id);
        Assert.NotNull(saved);
        Assert.Equal("Original title", saved!.Title);
        Assert.Equal(ProposalStatus.Matched, saved.Status);
    }
}
