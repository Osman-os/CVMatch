using CVMatch.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CVMatch.Web.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<CvSubmission> CvSubmissions => Set<CvSubmission>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<JobPostingSkill> JobPostingSkills => Set<JobPostingSkill>();
    public DbSet<CandidateNote> CandidateNotes => Set<CandidateNote>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---------- City ----------
        builder.Entity<City>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
        });

        // ---------- Skill ----------
        builder.Entity<Skill>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
        });

        // ---------- CandidateProfile ----------
        builder.Entity<CandidateProfile>(e =>
        {
            e.Property(x => x.ApplicationReferenceNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.Property(x => x.PhoneNumber).HasMaxLength(30);
            e.Property(x => x.PhoneNormalized).HasMaxLength(20);
            e.HasIndex(x => x.PhoneNormalized);
            e.HasIndex(x => x.Email);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.LinkedInUrl).HasMaxLength(300);
            e.Property(x => x.GitHubUrl).HasMaxLength(300);
            e.Property(x => x.EditTokenHash).IsRequired().HasMaxLength(128);
            e.Property(x => x.PhotoFileName).HasMaxLength(100);
            e.HasIndex(x => x.PreferredEmploymentType);
            e.HasIndex(x => x.ApplicationReferenceNumber).IsUnique();
            e.HasIndex(x => x.EditTokenHash).IsUnique();
            e.HasIndex(x => x.Status);

            e.HasOne(x => x.City)
             .WithMany(c => c.Candidates)
             .HasForeignKey(x => x.CityId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.JobPostingId);

            e.HasOne(x => x.JobPosting)
             .WithMany(j => j.Candidates)
             .HasForeignKey(x => x.JobPostingId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Education ----------
        builder.Entity<Education>(e =>
        {
            e.Property(x => x.School).IsRequired().HasMaxLength(200);
            e.Property(x => x.FieldOfStudy).HasMaxLength(150);
            e.HasIndex(x => x.CandidateProfileId);

            e.HasOne(x => x.CandidateProfile)
             .WithMany(c => c.Educations)
             .HasForeignKey(x => x.CandidateProfileId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- WorkExperience ----------
        builder.Entity<WorkExperience>(e =>
        {
            e.Property(x => x.CompanyName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Position).HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(1000);

            e.HasOne(x => x.CandidateProfile)
             .WithMany(c => c.WorkExperiences)
             .HasForeignKey(x => x.CandidateProfileId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Project ----------
        builder.Entity<Project>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Technologies).HasMaxLength(300);
            e.Property(x => x.Url).HasMaxLength(300);

            e.HasOne(x => x.CandidateProfile)
             .WithMany(c => c.Projects)
             .HasForeignKey(x => x.CandidateProfileId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- CvSubmission ----------
        builder.Entity<CvSubmission>(e =>
        {
            e.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(260);
            e.Property(x => x.StoredFileName).IsRequired().HasMaxLength(100);
            e.Property(x => x.PreviewImageFileName).HasMaxLength(100);
            e.Property(x => x.ErrorMessage).HasMaxLength(1000);
            e.Property(x => x.PhotoFileName).HasMaxLength(100);

            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => new { x.Status, x.ExpiresAt });
            e.HasIndex(x => x.CandidateProfileId);

            // Eşzamanlı güncellemeleri yakalamak için sürüm damgası
            e.Property(x => x.RowVersion).IsRowVersion();

            e.HasOne(x => x.CandidateProfile)
             .WithMany(c => c.CvSubmissions)
             .HasForeignKey(x => x.CandidateProfileId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.JobPostingId);

            e.HasOne(x => x.JobPosting)
             .WithMany(j => j.CvSubmissions)
             .HasForeignKey(x => x.JobPostingId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- JobPosting ----------
        builder.Entity<JobPosting>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);

            e.HasIndex(x => x.Status);

            e.HasOne(x => x.City)
             .WithMany(c => c.JobPostings)
             .HasForeignKey(x => x.CityId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- CandidateNote ----------
        builder.Entity<CandidateNote>(e =>
        {
            e.Property(x => x.Content).IsRequired().HasMaxLength(2000);
            e.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(450);
            e.Property(x => x.CreatedByEmail).IsRequired().HasMaxLength(256);

            e.HasIndex(x => new { x.CandidateProfileId, x.CreatedAt });

            e.HasOne(x => x.CandidateProfile)
             .WithMany(c => c.Notes)
             .HasForeignKey(x => x.CandidateProfileId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- CandidateSkill (çoktan çoğa) ----------
        builder.Entity<CandidateSkill>(e =>
        {
            e.HasKey(x => new { x.CandidateProfileId, x.SkillId });

            e.HasOne(x => x.CandidateProfile)
             .WithMany(c => c.CandidateSkills)
             .HasForeignKey(x => x.CandidateProfileId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Skill)
             .WithMany(s => s.CandidateSkills)
             .HasForeignKey(x => x.SkillId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- JobPostingSkill (çoktan çoğa) ----------
        builder.Entity<JobPostingSkill>(e =>
        {
            e.HasKey(x => new { x.JobPostingId, x.SkillId });

            e.HasOne(x => x.JobPosting)
             .WithMany(j => j.JobPostingSkills)
             .HasForeignKey(x => x.JobPostingId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Skill)
             .WithMany(s => s.JobPostingSkills)
             .HasForeignKey(x => x.SkillId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}