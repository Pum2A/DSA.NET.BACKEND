using DSA.Models;
using Microsoft.EntityFrameworkCore;

namespace DSA.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Module> Modules { get; set; } = null!;
        public DbSet<Lesson> Lessons { get; set; } = null!;
        public DbSet<LessonStep> LessonSteps { get; set; } = null!;
        public DbSet<UserProgress> UserProgresses { get; set; } = null!;
        public DbSet<StepProgress> StepProgresses { get; set; } = null!;
        public DbSet<Quiz> Quizzes { get; set; } = null!;
        public DbSet<QuizQuestion> QuizQuestions { get; set; } = null!;
        public DbSet<QuizOption> QuizOptions { get; set; } = null!;
        public DbSet<QuizResult> QuizResults { get; set; } = null!;
        public DbSet<QuizAnswer> QuizAnswers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fix column mappings for User entity
            modelBuilder.Entity<User>(entity =>
            {
                // Explicitly map column names to match property names
                entity.Property(e => e.Username).HasColumnName("Username");
                entity.Property(e => e.Avatar).HasColumnName("Avatar");
                entity.Property(e => e.XpPoints).HasColumnName("XpPoints");

                // UUID configuration
                entity.Property(e => e.Id)
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("gen_random_uuid()");
            });

            // Configure UUID primary keys for entities
            ConfigureUuidPrimaryKey<Quiz>(modelBuilder);
            ConfigureUuidPrimaryKey<QuizQuestion>(modelBuilder);
            ConfigureUuidPrimaryKey<QuizOption>(modelBuilder);
            ConfigureUuidPrimaryKey<QuizResult>(modelBuilder);
            ConfigureUuidPrimaryKey<QuizAnswer>(modelBuilder);
            ConfigureUuidPrimaryKey<Module>(modelBuilder);
            ConfigureUuidPrimaryKey<Lesson>(modelBuilder);
            ConfigureUuidPrimaryKey<LessonStep>(modelBuilder);
            ConfigureUuidPrimaryKey<UserProgress>(modelBuilder);
            ConfigureUuidPrimaryKey<StepProgress>(modelBuilder);
            ConfigureUuidPrimaryKey<RefreshToken>(modelBuilder);

            // Handle list of GUIDs in QuizAnswer
            modelBuilder.Entity<QuizAnswer>()
                .Property(qa => qa.SelectedOptionIds)
                .HasColumnType("jsonb");

            // Configure entity relationships
            modelBuilder.Entity<User>()
                .HasMany(u => u.LessonProgresses)
                .WithOne(up => up.User)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.QuizResults)
                .WithOne(qr => qr.User)
                .HasForeignKey(qr => qr.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId);

            modelBuilder.Entity<Module>()
                .HasMany(m => m.Lessons)
                .WithOne(l => l.Module)
                .HasForeignKey(l => l.ModuleId);

            modelBuilder.Entity<Module>()
                .HasMany(m => m.Quizzes)
                .WithOne(q => q.Module)
                .HasForeignKey(q => q.ModuleId);

            modelBuilder.Entity<Lesson>()
                .HasMany(l => l.Steps)
                .WithOne(ls => ls.Lesson)
                .HasForeignKey(ls => ls.LessonId);

            modelBuilder.Entity<Lesson>()
                .HasMany(l => l.UserProgresses)
                .WithOne(up => up.Lesson)
                .HasForeignKey(up => up.LessonId);

            modelBuilder.Entity<UserProgress>()
                .HasMany(up => up.StepProgresses)
                .WithOne(sp => sp.UserProgress)
                .HasForeignKey(sp => sp.UserProgressId);

            modelBuilder.Entity<Quiz>()
                .HasMany(q => q.Questions)
                .WithOne(qq => qq.Quiz)
                .HasForeignKey(qq => qq.QuizId);

            modelBuilder.Entity<Quiz>()
                .HasMany(q => q.UserResults)
                .WithOne(qr => qr.Quiz)
                .HasForeignKey(qr => qr.QuizId);

            modelBuilder.Entity<QuizQuestion>()
                .HasMany(qq => qq.Options)
                .WithOne(qo => qo.Question)
                .HasForeignKey(qo => qo.QuestionId);

            modelBuilder.Entity<QuizResult>()
                .HasMany(qr => qr.Answers)
                .WithOne(qa => qa.QuizResult)
                .HasForeignKey(qa => qa.QuizResultId);
        }

        // Helper method to configure UUID primary keys
        private void ConfigureUuidPrimaryKey<T>(ModelBuilder modelBuilder) where T : class
        {
            modelBuilder.Entity<T>().Property("Id")
                .HasColumnType("uuid")
                .HasDefaultValueSql("gen_random_uuid()");
        }
    }
}