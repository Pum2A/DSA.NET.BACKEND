// DSA.Infrastructure/Data/ApplicationDbContext.cs
using DSA.Core.Entities;
using DSA.Core.Entities.Learning;
using DSA.Core.Entities.Quiz;
using DSA.Core.Entities.Interactive;
using DSA.Core.Entities.User;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DSA.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Learning Module
        public DbSet<Module> Modules { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Step> Steps { get; set; }
        public DbSet<QuizOption> QuizOptions { get; set; }
        public DbSet<TestCase> TestCases { get; set; }
        public DbSet<ListItem> ListItems { get; set; }

        // User Related
        public DbSet<UserProgress> UserProgress { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ContentActivityLog> ContentActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Identity configuration
            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");

            // Module configurations
            modelBuilder.Entity<Module>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ExternalId).IsRequired();
                entity.HasIndex(e => e.ExternalId).IsUnique();
                entity.HasMany(e => e.Lessons)
                    .WithOne(e => e.Module)
                    .HasForeignKey(e => e.ModuleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Lesson configurations
            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ExternalId).IsRequired();
                entity.HasIndex(e => e.ExternalId).IsUnique();
                entity.HasMany(e => e.Steps)
                    .WithOne(e => e.Lesson)
                    .HasForeignKey(e => e.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Step configurations
            modelBuilder.Entity<Step>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Code).IsRequired(false);
                entity.Property(e => e.Language).IsRequired(false);
                entity.Property(e => e.ImageUrl).IsRequired(false);
                entity.Property(e => e.AdditionalData).IsRequired(false);

                entity.HasMany(e => e.QuizOptions)
                    .WithOne(e => e.Step)
                    .HasForeignKey(e => e.StepId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.TestCases)
                    .WithOne(e => e.Step)
                    .HasForeignKey(e => e.StepId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.ListItems)
                    .WithOne(e => e.Step)
                    .HasForeignKey(e => e.StepId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // QuizOption configurations
            modelBuilder.Entity<QuizOption>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired();
            });

            // TestCase configurations
            modelBuilder.Entity<TestCase>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Input).IsRequired();
                entity.Property(e => e.ExpectedOutput).IsRequired();
            });

            // ListItem configurations
            modelBuilder.Entity<ListItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired();
            });

            // UserProgress configurations
            modelBuilder.Entity<UserProgress>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LessonId });

                entity.HasOne(e => e.User)
                    .WithMany(e => e.UserProgresses)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Lesson)
                    .WithMany()
                    .HasForeignKey(e => e.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserActivity configurations
            modelBuilder.Entity<UserActivity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ActionType).IsRequired();
                entity.Property(e => e.ActionTime).IsRequired();
            });

            // Notification configurations
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Type).IsRequired();
            });

            // ContentActivityLog configurations
            modelBuilder.Entity<ContentActivityLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired();
                entity.Property(e => e.ContentType).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
            });
        }
    }
}