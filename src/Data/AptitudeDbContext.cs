using Microsoft.EntityFrameworkCore;
using Webster.AptitudePortal.Api.Models.Entities;

namespace Webster.AptitudePortal.Api.Data;

public class AptitudeDbContext : DbContext
{
    public AptitudeDbContext(DbContextOptions<AptitudeDbContext> options) : base(options)
    {
    }

    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateEducation> CandidateEducations => Set<CandidateEducation>();
    public DbSet<CandidateExperience> CandidateExperiences => Set<CandidateExperience>();
    public DbSet<TestSection> TestSections => Set<TestSection>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<CandidateResponse> CandidateResponses => Set<CandidateResponse>();
    public DbSet<AptiClearedCandidate> AptiClearedCandidates => Set<AptiClearedCandidate>();
    public DbSet<AssessmentAuditLog> AssessmentAuditLogs => Set<AssessmentAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Managers
        modelBuilder.Entity<Manager>(entity =>
        {
            entity.HasKey(m => m.ManagerId);
            entity.HasIndex(m => m.Email).IsUnique();
        });

        // 2. Candidates
        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.HasKey(c => c.CandidateId);
            entity.HasIndex(c => c.RegistrationNumber).IsUnique();
            entity.HasIndex(c => c.Email).IsUnique();
            entity.HasIndex(c => c.Phone).IsUnique();
            entity.HasIndex(c => c.Username).IsUnique();

            entity.HasOne(c => c.CreatedByManager)
                .WithMany(m => m.CandidatesEnrolled)
                .HasForeignKey(c => c.CreatedByManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 3. CandidateEducations
        modelBuilder.Entity<CandidateEducation>(entity =>
        {
            entity.HasKey(e => e.EducationId);
            entity.Property(e => e.PercentageOrCGPA).HasPrecision(5, 2);

            entity.HasOne(e => e.Candidate)
                .WithMany(c => c.Educations)
                .HasForeignKey(e => e.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 4. CandidateExperiences
        modelBuilder.Entity<CandidateExperience>(entity =>
        {
            entity.HasKey(x => x.ExperienceId);
            entity.Property(x => x.TotalYears).HasPrecision(4, 1);

            entity.HasOne(x => x.Candidate)
                .WithMany(c => c.Experiences)
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 5. TestSections
        modelBuilder.Entity<TestSection>(entity =>
        {
            entity.HasKey(s => s.SectionId);
            entity.HasIndex(s => s.SectionName).IsUnique();
            entity.HasIndex(s => s.SequenceOrder).IsUnique();
            entity.Property(s => s.CutoffPercentage).HasPrecision(5, 2);
        });

        // 6. Questions
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(q => q.QuestionId);

            entity.HasOne(q => q.Section)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(q => q.CreatedByManager)
                .WithMany(m => m.QuestionsCreated)
                .HasForeignKey(q => q.CreatedByManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 7. QuestionOptions
        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.HasKey(o => o.OptionId);
            entity.HasIndex(o => new { o.QuestionId, o.OptionLabel }).IsUnique();

            entity.HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 8. TestAttempts
        modelBuilder.Entity<TestAttempt>(entity =>
        {
            entity.HasKey(a => a.AttemptId);
            entity.HasIndex(a => a.CandidateId).IsUnique();
            entity.Property(a => a.TotalScoreObtained).HasPrecision(6, 2);
            entity.Property(a => a.MaxPossibleScore).HasPrecision(6, 2);
            entity.Property(a => a.PercentageScore).HasPrecision(5, 2);

            entity.HasOne(a => a.Candidate)
                .WithOne(c => c.TestAttempt)
                .HasForeignKey<TestAttempt>(a => a.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.CurrentSection)
                .WithMany()
                .HasForeignKey(a => a.CurrentSectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 9. CandidateResponses
        modelBuilder.Entity<CandidateResponse>(entity =>
        {
            entity.HasKey(r => r.ResponseId);
            entity.HasIndex(r => new { r.AttemptId, r.QuestionId }).IsUnique();
            entity.Property(r => r.MarksAwarded).HasPrecision(4, 2);

            entity.HasOne(r => r.Attempt)
                .WithMany(a => a.Responses)
                .HasForeignKey(r => r.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Question)
                .WithMany()
                .HasForeignKey(r => r.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.SelectedOption)
                .WithMany()
                .HasForeignKey(r => r.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 10. AptiClearedCandidates
        modelBuilder.Entity<AptiClearedCandidate>(entity =>
        {
            entity.HasKey(acc => acc.ClearanceId);
            entity.HasIndex(acc => acc.CandidateId).IsUnique();
            entity.HasIndex(acc => acc.AttemptId).IsUnique();
            entity.Property(acc => acc.OverallScore).HasPrecision(6, 2);
            entity.Property(acc => acc.PercentageScore).HasPrecision(5, 2);

            entity.HasOne(acc => acc.Candidate)
                .WithOne(c => c.AptiClearedRecord)
                .HasForeignKey<AptiClearedCandidate>(acc => acc.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(acc => acc.Attempt)
                .WithOne(a => a.AptiClearedRecord)
                .HasForeignKey<AptiClearedCandidate>(acc => acc.AttemptId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 11. AssessmentAuditLogs
        modelBuilder.Entity<AssessmentAuditLog>(entity =>
        {
            entity.HasKey(log => log.LogId);
        });
    }
}
