using DSA.Core.Entities;
using DSA.Infrastructure.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using System.Reflection.Emit;

namespace DSA.Infrastructure
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        // DbSety dla modułu lekcji
        public DbSet<Module> Modules { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Step> Steps { get; set; }
        public DbSet<UserProgress> UserProgresses { get; set; }

        public DbSet<UserActivity> UserActivities { get; set; }

        public DbSet<Notification> Notifications { get; set; }


        public DbSet<ContentActivityLog> ContentActivityLogs { get; set; }

        // I dodaj alias dla zachowania wstecznej kompatybilności
        public DbSet<UserProgress> UserProgress => UserProgresses;



        // Opcjonalnie, jeśli implementujesz quizy/testy
        //public DbSet<Quiz> Quizzes { get; set; }
        //public DbSet<QuizQuestion> QuizQuestions { get; set; }
        //public DbSet<QuizOption> QuizOptions { get; set; }
        //public DbSet<UserQuizAttempt> UserQuizAttempts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfiguracje dla modeli związanych z lekcjami

            // 1. Relacja Module -> Lessons (jeden do wielu)
            modelBuilder.Entity<Step>()
         .Property(s => s.Code)
         .IsRequired(false); // To oznacza nullable

            modelBuilder.Entity<Step>()
                .Property(s => s.Language)
                .IsRequired(false);

            modelBuilder.Entity<Step>()
                .Property(s => s.ImageUrl)
                .IsRequired(false);

            modelBuilder.Entity<Step>()
                .Property(s => s.AdditionalData)
                .IsRequired(false);

            modelBuilder.Entity<UserProgress>(entity =>
            {
                entity.HasKey(up => new { up.UserId, up.LessonId }); // Example composite key
                entity.HasOne(up => up.User)
                      .WithMany(u => u.UserProgresses)
                      .HasForeignKey(up => up.UserId);
                entity.HasOne(up => up.Lesson)
                      .WithMany()
                      .HasForeignKey(up => up.LessonId);
            });


        }
    }

}