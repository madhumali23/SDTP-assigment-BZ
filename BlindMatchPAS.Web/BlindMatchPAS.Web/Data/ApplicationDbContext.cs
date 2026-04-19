using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BlindMatchPAS.Web.Models;

namespace BlindMatchPAS.Web.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProjectProposal> ProjectProposals => Set<ProjectProposal>();
    public DbSet<ResearchArea> ResearchAreas => Set<ResearchArea>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<SupervisorProfile> SupervisorProfiles => Set<SupervisorProfile>();
    public DbSet<ProposalTechStack> ProposalTechStacks => Set<ProposalTechStack>();
    public DbSet<SupervisorExpertise> SupervisorExpertise => Set<SupervisorExpertise>();
    public DbSet<SupervisorInterest> SupervisorInterests => Set<SupervisorInterest>();
    public DbSet<MatchAssignment> MatchAssignments => Set<MatchAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ProjectProposal>(entity =>
        {
            entity.HasIndex(p => new { p.StudentUserId, p.Status });
            entity.Property(p => p.Title).HasMaxLength(150);
            entity.Property(p => p.Abstract).HasMaxLength(2000);
            entity.Property(p => p.TechnicalStack).HasMaxLength(300);
            entity.Property(p => p.ResearchArea).HasMaxLength(120);
            entity.Property(p => p.StudentUserId).HasMaxLength(450);
        });

        builder.Entity<ResearchArea>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
            entity.Property(r => r.Name).HasMaxLength(120);
            entity.Property(r => r.Description).HasMaxLength(500);
        });

        builder.Entity<StudentProfile>(entity =>
        {
            entity.HasIndex(p => p.UserId).IsUnique();
            entity.Property(p => p.UserId).HasMaxLength(450);
            entity.Property(p => p.FullName).HasMaxLength(120);
            entity.Property(p => p.Department).HasMaxLength(120);
        });

        builder.Entity<SupervisorProfile>(entity =>
        {
            entity.HasIndex(p => p.UserId).IsUnique();
            entity.Property(p => p.UserId).HasMaxLength(450);
            entity.Property(p => p.DisplayName).HasMaxLength(120);
            entity.Property(p => p.Bio).HasMaxLength(1000);
        });

        builder.Entity<ProposalTechStack>(entity =>
        {
            entity.HasIndex(t => new { t.ProjectProposalId, t.Name }).IsUnique();
            entity.Property(t => t.Name).HasMaxLength(80);
        });

        builder.Entity<SupervisorExpertise>(entity =>
        {
            entity.HasIndex(e => new { e.SupervisorUserId, e.ResearchArea }).IsUnique();
            entity.Property(e => e.SupervisorUserId).HasMaxLength(450);
            entity.Property(e => e.ResearchArea).HasMaxLength(120);
        });

        builder.Entity<SupervisorInterest>(entity =>
        {
            entity.HasIndex(i => new { i.SupervisorUserId, i.ProjectProposalId }).IsUnique();
            entity.Property(i => i.SupervisorUserId).HasMaxLength(450);
        });

        builder.Entity<MatchAssignment>(entity =>
        {
            entity.HasIndex(m => m.ProjectProposalId).IsUnique();
            entity.Property(m => m.StudentUserId).HasMaxLength(450);
            entity.Property(m => m.SupervisorUserId).HasMaxLength(450);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => a.CreatedAtUtc);
            entity.Property(a => a.ActorUserId).HasMaxLength(450);
            entity.Property(a => a.ActionType).HasMaxLength(100);
            entity.Property(a => a.EntityName).HasMaxLength(120);
            entity.Property(a => a.EntityId).HasMaxLength(120);
            entity.Property(a => a.Details).HasMaxLength(1000);
        });
    }
}
