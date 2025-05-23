using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DSA.Core.Entities.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DSA.Infrastructure.Data
{
    public static class JsonSeeder
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                logger.LogInformation("Seedowanie danych z JSON...");

                // Sprawdź i dodaj moduły jeśli potrzebne
                if (!await context.Modules.AnyAsync())
                {
                    await SeedModulesAsync(context, logger);
                    await SeedLessonsAsync(context, logger);
                }

                // Dodaj kroki do bubble-sort
                await SeedBubbleSortStepsAsync(context, logger);

                logger.LogInformation("Seedowanie zakończone");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Błąd seedowania danych");
                throw;
            }
        }

        private static async Task SeedModulesAsync(ApplicationDbContext context, ILogger logger)
        {
            var modules = await ReadFromJsonFileAsync<Module>("SeedData/modules.json");
            if (modules?.Count > 0)
            {
                logger.LogInformation($"Dodawanie {modules.Count} modułów");

                // Inicjalizacja kolekcji
                foreach (var module in modules)
                    module.Prerequisites ??= new List<string>();

                context.Modules.AddRange(modules);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedLessonsAsync(ApplicationDbContext context, ILogger logger)
        {
            var lessons = await ReadFromJsonFileAsync<Lesson>("SeedData/lessons.json");
            if (lessons?.Count > 0)
            {
                logger.LogInformation($"Dodawanie {lessons.Count} lekcji");

                // Inicjalizacja kolekcji
                foreach (var lesson in lessons)
                    lesson.RequiredSkills ??= new List<string>();

                context.Lessons.AddRange(lessons);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedBubbleSortStepsAsync(ApplicationDbContext context, ILogger logger)
        {
            // Znajdź lekcję bubble-sort
            var bubbleSortLesson = await context.Lessons
                .FirstOrDefaultAsync(l => l.ExternalId == "bubble-sort");

            if (bubbleSortLesson == null)
            {
                logger.LogWarning("Nie znaleziono lekcji bubble-sort");
                return;
            }

            // Sprawdź czy ma kroki
            if (await context.Steps.AnyAsync(s => s.LessonId == bubbleSortLesson.Id))
            {
                logger.LogInformation("Lekcja bubble-sort ma już kroki");
                return;
            }

            // Dodaj kroki
            var steps = await ReadFromJsonFileAsync<Step>("SeedData/steps.json");
            if (steps?.Count > 0)
            {
                logger.LogInformation($"Dodawanie {steps.Count} kroków do bubble-sort");

                // Przypisz właściwe LessonId
                foreach (var step in steps)
                    step.LessonId = bubbleSortLesson.Id;

                context.Steps.AddRange(steps);
                await context.SaveChangesAsync();
            }
        }

        private static async Task<List<T>> ReadFromJsonFileAsync<T>(string filePath) where T : class
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<List<T>>(json, _options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd odczytu {filePath}: {ex.Message}");
                return null;
            }
        }
    }
}