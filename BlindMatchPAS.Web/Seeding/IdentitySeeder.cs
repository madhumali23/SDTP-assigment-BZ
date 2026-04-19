using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using BlindMatchPAS.Web.Data;
using BlindMatchPAS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Web.Seeding;

public static class IdentitySeeder
{
    private static readonly string[] Roles =
    [
        "Student",
        "Supervisor",
        "ModuleLeader",
        "SystemAdministrator"
    ];

    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var demoPassword = configuration["SeedUsers:DefaultPassword"];
        if (string.IsNullOrWhiteSpace(demoPassword))
        {
            return;
        }

        await EnsureUserInRoleAsync(db, userManager, configuration["SeedUsers:StudentEmail"], demoPassword, "Student");
        await EnsureUserInRoleAsync(db, userManager, configuration["SeedUsers:SupervisorEmail"], demoPassword, "Supervisor");
        await EnsureUserInRoleAsync(db, userManager, configuration["SeedUsers:ModuleLeaderEmail"], demoPassword, "ModuleLeader");
        await EnsureUserInRoleAsync(db, userManager, configuration["SeedUsers:SystemAdminEmail"], demoPassword, "SystemAdministrator");

        await SeedDomainDataAsync(db, userManager, demoPassword);
    }

    private static async Task SeedDomainDataAsync(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        string password)
    {
        var student1 = await EnsureStudentAsync(db, userManager, "student1@blindmatch.local", password, "Nimal Perera", "Computing");
        var student2 = await EnsureStudentAsync(db, userManager, "student2@blindmatch.local", password, "Kasuni Silva", "Information Systems");
        var student3 = await EnsureStudentAsync(db, userManager, "student3@blindmatch.local", password, "Ravindu Fernando", "Software Engineering");

        var supervisor1 = await EnsureSupervisorAsync(
            db,
            userManager,
            "supervisor1@blindmatch.local",
            password,
            "Dr. D. Jayasuriya",
            "Focuses on machine learning systems and applied AI engineering.");

        var supervisor2 = await EnsureSupervisorAsync(
            db,
            userManager,
            "supervisor2@blindmatch.local",
            password,
            "Dr. M. Senanayake",
            "Specializes in cyber security, threat modeling and secure software design.");

        var supervisor3 = await EnsureSupervisorAsync(
            db,
            userManager,
            "supervisor3@blindmatch.local",
            password,
            "Dr. A. Hettiarachchi",
            "Works on cloud-native architectures and DevOps automation.");

        await SeedResearchAreasAsync(db);
        await SeedSupervisorExpertiseAsync(db, supervisor1.Id, supervisor2.Id, supervisor3.Id);

        var proposal1 = await EnsureProposalAsync(
            db,
            student1.Id,
            "Explainable AI for Student Performance Prediction",
            "Build an explainable machine learning model to predict at-risk students and surface interpretable insights for lecturers.",
            "ASP.NET Core, Python, Scikit-Learn, SQL Server",
            "Artificial Intelligence",
            ProposalStatus.Matched);

        var proposal2 = await EnsureProposalAsync(
            db,
            student2.Id,
            "Secure Campus Helpdesk Portal with Threat Detection",
            "Design a secure ticketing platform with anomaly detection and role-based access control for campus IT operations.",
            "ASP.NET Core, SQL Server, OWASP ZAP, Docker",
            "Cyber Security",
            ProposalStatus.UnderReview);

        var proposal3 = await EnsureProposalAsync(
            db,
            student3.Id,
            "Cloud-Native Research Repository",
            "Create a scalable repository for research artifacts with CI/CD and container-based deployments.",
            "ASP.NET Core, Docker, GitHub Actions, Azure",
            "Cloud Computing",
            ProposalStatus.Pending);

        await SeedProposalTechStacksAsync(db, proposal1.Id, ["ASP.NET Core", "Python", "Scikit-Learn", "SQL Server"]);
        await SeedProposalTechStacksAsync(db, proposal2.Id, ["ASP.NET Core", "SQL Server", "OWASP ZAP", "Docker"]);
        await SeedProposalTechStacksAsync(db, proposal3.Id, ["ASP.NET Core", "Docker", "GitHub Actions", "Azure"]);

        await SeedSupervisorInterestsAsync(db, supervisor1.Id, supervisor2.Id, supervisor3.Id, proposal1.Id, proposal2.Id, proposal3.Id);
        await SeedMatchedAssignmentAsync(db, proposal1.Id, student1.Id, supervisor1.Id);
        await SeedAuditLogsAsync(db, student1.Id, supervisor1.Id, proposal1.Id, proposal2.Id, proposal3.Id);
    }

    private static async Task<IdentityUser> EnsureStudentAsync(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        string fullName,
        string department)
    {
        await EnsureUserInRoleAsync(db, userManager, email, password, "Student");
        var user = await userManager.FindByEmailAsync(email) ?? throw new InvalidOperationException($"Unable to seed student user: {email}");

        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile is null)
        {
            db.StudentProfiles.Add(new StudentProfile
            {
                UserId = user.Id,
                FullName = fullName,
                Department = department,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            profile.FullName = fullName;
            profile.Department = department;
        }

        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<IdentityUser> EnsureSupervisorAsync(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        string displayName,
        string bio)
    {
        await EnsureUserInRoleAsync(db, userManager, email, password, "Supervisor");
        var user = await userManager.FindByEmailAsync(email) ?? throw new InvalidOperationException($"Unable to seed supervisor user: {email}");

        var profile = await db.SupervisorProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile is null)
        {
            db.SupervisorProfiles.Add(new SupervisorProfile
            {
                UserId = user.Id,
                DisplayName = displayName,
                Bio = bio,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            profile.DisplayName = displayName;
            profile.Bio = bio;
        }

        await db.SaveChangesAsync();
        return user;
    }

    private static async Task SeedResearchAreasAsync(ApplicationDbContext db)
    {
        string[] researchAreas =
        [
            "Artificial Intelligence",
            "Cyber Security",
            "Cloud Computing",
            "Software Engineering",
            "Data Science"
        ];

        foreach (var area in researchAreas)
        {
            var exists = await db.ResearchAreas.AnyAsync(r => r.Name == area);
            if (!exists)
            {
                db.ResearchAreas.Add(new ResearchArea
                {
                    Name = area,
                    Description = $"Seeded default research area: {area}",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedSupervisorExpertiseAsync(
        ApplicationDbContext db,
        string supervisor1Id,
        string supervisor2Id,
        string supervisor3Id)
    {
        await EnsureExpertiseAsync(db, supervisor1Id, "Artificial Intelligence");
        await EnsureExpertiseAsync(db, supervisor1Id, "Data Science");
        await EnsureExpertiseAsync(db, supervisor2Id, "Cyber Security");
        await EnsureExpertiseAsync(db, supervisor3Id, "Cloud Computing");
        await EnsureExpertiseAsync(db, supervisor3Id, "Software Engineering");
    }

    private static async Task EnsureExpertiseAsync(ApplicationDbContext db, string supervisorId, string area)
    {
        var exists = await db.SupervisorExpertise.AnyAsync(e => e.SupervisorUserId == supervisorId && e.ResearchArea == area);
        if (exists)
        {
            return;
        }

        db.SupervisorExpertise.Add(new SupervisorExpertise
        {
            SupervisorUserId = supervisorId,
            ResearchArea = area,
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static async Task<ProjectProposal> EnsureProposalAsync(
        ApplicationDbContext db,
        string studentUserId,
        string title,
        string abstractText,
        string technicalStack,
        string researchArea,
        ProposalStatus status)
    {
        var proposal = await db.ProjectProposals.FirstOrDefaultAsync(p => p.Title == title && p.StudentUserId == studentUserId);
        if (proposal is null)
        {
            proposal = new ProjectProposal
            {
                StudentUserId = studentUserId,
                Title = title,
                Abstract = abstractText,
                TechnicalStack = technicalStack,
                ResearchArea = researchArea,
                Status = status,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.ProjectProposals.Add(proposal);
        }
        else
        {
            proposal.Abstract = abstractText;
            proposal.TechnicalStack = technicalStack;
            proposal.ResearchArea = researchArea;
            proposal.Status = status;
            proposal.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return proposal;
    }

    private static async Task SeedProposalTechStacksAsync(ApplicationDbContext db, int projectProposalId, string[] techItems)
    {
        foreach (var tech in techItems)
        {
            var exists = await db.ProposalTechStacks.AnyAsync(t => t.ProjectProposalId == projectProposalId && t.Name == tech);
            if (!exists)
            {
                db.ProposalTechStacks.Add(new ProposalTechStack
                {
                    ProjectProposalId = projectProposalId,
                    Name = tech,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedSupervisorInterestsAsync(
        ApplicationDbContext db,
        string supervisor1Id,
        string supervisor2Id,
        string supervisor3Id,
        int proposal1Id,
        int proposal2Id,
        int proposal3Id)
    {
        await EnsureInterestAsync(db, supervisor1Id, proposal1Id);
        await EnsureInterestAsync(db, supervisor2Id, proposal2Id);
        await EnsureInterestAsync(db, supervisor3Id, proposal3Id);
        await EnsureInterestAsync(db, supervisor1Id, proposal3Id);
    }

    private static async Task EnsureInterestAsync(ApplicationDbContext db, string supervisorId, int proposalId)
    {
        var exists = await db.SupervisorInterests.AnyAsync(i => i.SupervisorUserId == supervisorId && i.ProjectProposalId == proposalId);
        if (exists)
        {
            return;
        }

        db.SupervisorInterests.Add(new SupervisorInterest
        {
            SupervisorUserId = supervisorId,
            ProjectProposalId = proposalId,
            ExpressedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedMatchedAssignmentAsync(
        ApplicationDbContext db,
        int proposalId,
        string studentUserId,
        string supervisorUserId)
    {
        var hasAssignment = await db.MatchAssignments.AnyAsync(a => a.ProjectProposalId == proposalId);
        if (!hasAssignment)
        {
            db.MatchAssignments.Add(new MatchAssignment
            {
                ProjectProposalId = proposalId,
                StudentUserId = studentUserId,
                SupervisorUserId = supervisorUserId,
                ConfirmedAtUtc = DateTime.UtcNow
            });
        }

        var proposal = await db.ProjectProposals.FirstOrDefaultAsync(p => p.Id == proposalId);
        if (proposal is not null)
        {
            proposal.Status = ProposalStatus.Matched;
            proposal.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedAuditLogsAsync(
        ApplicationDbContext db,
        string studentUserId,
        string supervisorUserId,
        int proposal1Id,
        int proposal2Id,
        int proposal3Id)
    {
        await EnsureAuditLogAsync(db, studentUserId, "ProposalCreated", "ProjectProposal", proposal1Id.ToString(), "Student submitted proposal for explainable AI.");
        await EnsureAuditLogAsync(db, studentUserId, "ProposalCreated", "ProjectProposal", proposal2Id.ToString(), "Student submitted proposal for secure helpdesk portal.");
        await EnsureAuditLogAsync(db, studentUserId, "ProposalCreated", "ProjectProposal", proposal3Id.ToString(), "Student submitted proposal for cloud-native repository.");
        await EnsureAuditLogAsync(db, supervisorUserId, "InterestExpressed", "ProjectProposal", proposal1Id.ToString(), "Supervisor expressed interest in AI proposal.");
        await EnsureAuditLogAsync(db, supervisorUserId, "MatchConfirmed", "MatchAssignment", proposal1Id.ToString(), "Supervisor confirmed assignment for matched proposal.");
    }

    private static async Task EnsureAuditLogAsync(
        ApplicationDbContext db,
        string actorUserId,
        string actionType,
        string entityName,
        string entityId,
        string details)
    {
        var exists = await db.AuditLogs.AnyAsync(a =>
            a.ActorUserId == actorUserId
            && a.ActionType == actionType
            && a.EntityName == entityName
            && a.EntityId == entityId);

        if (exists)
        {
            return;
        }

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            ActionType = actionType,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserInRoleAsync(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        string? email,
        string password,
        string role)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                return;
            }
        }
        else
        {
            // Keep seeded demo users consistent even if the database already existed.
            // This avoids login issues caused by stale usernames/passwords/lockout state.
            var hasIdentityChanges = false;

            if (!string.Equals(user.UserName, email, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = email;
                hasIdentityChanges = true;
            }

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                hasIdentityChanges = true;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                hasIdentityChanges = true;
            }

            if (hasIdentityChanges)
            {
                await userManager.UpdateAsync(user);
            }

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetPasswordResult = await userManager.ResetPasswordAsync(user, resetToken, password);
            if (!resetPasswordResult.Succeeded)
            {
                var hasPassword = await userManager.HasPasswordAsync(user);
                if (!hasPassword)
                {
                    await userManager.AddPasswordAsync(user, password);
                }
            }

            await userManager.ResetAccessFailedCountAsync(user);
            await userManager.SetLockoutEndDateAsync(user, null);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        if (role == "Student")
        {
            var hasProfile = await db.StudentProfiles.AnyAsync(p => p.UserId == user.Id);
            if (!hasProfile)
            {
                db.StudentProfiles.Add(new StudentProfile
                {
                    UserId = user.Id,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        if (role == "Supervisor")
        {
            var hasProfile = await db.SupervisorProfiles.AnyAsync(p => p.UserId == user.Id);
            if (!hasProfile)
            {
                db.SupervisorProfiles.Add(new SupervisorProfile
                {
                    UserId = user.Id,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
