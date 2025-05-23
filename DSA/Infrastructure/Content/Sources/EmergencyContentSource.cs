using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Core.Entities.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DSA.Infrastructure.Content.Sources
{
    public class EmergencyContentSource : IContentSource
    {
        private readonly ILogger<EmergencyContentSource> _logger;

        public EmergencyContentSource(ILogger<EmergencyContentSource> logger) => _logger = logger;

        public async Task LoadContentAsync(ContentContext context)
        {
            // Sprawdź, czy istnieje jakakolwiek treść
            bool hasAnyModules = await context.DbContext.Modules.AnyAsync();

            if (!hasAnyModules)
                await CreateBasicModuleAsync(context);

            // Sprawdź problematyczną lekcję stack-queue
            await EnsureStackQueueHasStepsAsync(context);
        }

        private async Task CreateBasicModuleAsync(ContentContext context)
        {
            _logger.LogWarning("Brak modułów. Tworzę awaryjny moduł.");

            var emergencyModule = new Module
            {
                Title = "Podstawy struktur danych",
                Description = "Awaryjnie utworzony moduł wprowadzający do struktur danych",
                ExternalId = "emergency-module",
                Order = 1,
                Prerequisites = new List<string>()
            };

            context.DbContext.Modules.Add(emergencyModule);
            await context.DbContext.SaveChangesAsync();

            var emergencyLesson = new Lesson
            {
                Title = "Wstęp do struktur danych",
                Description = "Awaryjnie utworzona lekcja wprowadzająca",
                ExternalId = "emergency-lesson",
                ModuleId = emergencyModule.Id,
                XpReward = 10,
                RequiredSkills = new List<string>()
            };

            context.DbContext.Lessons.Add(emergencyLesson);
            await context.DbContext.SaveChangesAsync();

            var emergencyStep = new Step
            {
                LessonId = emergencyLesson.Id,
                Type = "text",
                Title = "Czym są struktury danych?",
                Content = "Struktury danych to sposoby organizowania i przechowywania danych w komputerze.",
                Order = 1
            };

            context.DbContext.Steps.Add(emergencyStep);
            await context.DbContext.SaveChangesAsync();
        }

        private async Task EnsureStackQueueHasStepsAsync(ContentContext context)
        {
            // Znajdź lekcję stack-queue
            var lesson = await context.DbContext.Lessons
                .FirstOrDefaultAsync(l => l.ExternalId == "stack-queue");

            if (lesson == null) return;

            // Sprawdź, czy lekcja ma kroki
            var hasSteps = await context.DbContext.Steps
                .AnyAsync(s => s.LessonId == lesson.Id);

            if (hasSteps) return;

            // Dodaj kroki dla stack-queue
            var steps = new List<Step>
            {
                new Step
                {
                    LessonId = lesson.Id,
                    Type = "text",
                    Title = "Wprowadzenie do stosów i kolejek",
                    Content = "Stosy i kolejki to podstawowe struktury danych.",
                    Order = 1
                },
                new Step
                {
                    LessonId = lesson.Id,
                    Type = "text",
                    Title = "Stos (Stack)",
                    Content = "Stos to struktura działająca na zasadzie LIFO.",
                    Order = 2
                }
            };

            context.DbContext.Steps.AddRange(steps);
            await context.DbContext.SaveChangesAsync();
        }
    }
}