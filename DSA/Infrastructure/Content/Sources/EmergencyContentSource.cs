using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DSA.Infrastructure.Content.Sources
{
    /// <summary>
    /// Awaryjne źródło treści, które dodaje podstawowe dane, jeśli ich brakuje
    /// </summary>
    public class EmergencyContentSource : IContentSource
    {
        private readonly ILogger<EmergencyContentSource> _logger;

        public EmergencyContentSource(ILogger<EmergencyContentSource> logger)
        {
            _logger = logger;
        }

        public async Task LoadContentAsync(ContentContext context)
        {
            _logger.LogInformation("Sprawdzanie czy istnieją podstawowe treści...");

            // Sprawdź, czy istnieje jakakolwiek treść
            bool hasAnyModules = await context.DbContext.Modules.AnyAsync();
            bool hasAnyLessons = await context.DbContext.Lessons.AnyAsync();

            if (!hasAnyModules)
            {
                await CreateBasicModuleAsync(context);
            }

            // Sprawdź problematyczną lekcję stack-queue
            await EnsureStackQueueHasStepsAsync(context);
        }

        private async Task CreateBasicModuleAsync(ContentContext context)
        {
            _logger.LogWarning("Brak modułów w bazie danych. Tworzę awaryjny moduł i lekcję.");

            var emergencyModule = new Module
            {
                Title = "Podstawy struktur danych",
                Description = "Awaryjnie utworzony moduł wprowadzający do struktur danych",
                ExternalId = "emergency-module",
                Order = 1
            };

            context.DbContext.Modules.Add(emergencyModule);
            await context.DbContext.SaveChangesAsync();

            var emergencyLesson = new Lesson
            {
                Title = "Wstęp do struktur danych",
                Description = "Awaryjnie utworzona lekcja wprowadzająca",
                ExternalId = "emergency-lesson",
                ModuleId = emergencyModule.Id,
                XpReward = 10
            };

            context.DbContext.Lessons.Add(emergencyLesson);
            await context.DbContext.SaveChangesAsync();

            var emergencyStep = new Step
            {
                LessonId = emergencyLesson.Id,
                Type = "text",
                Title = "Czym są struktury danych?",
                Content = "Struktury danych to sposoby organizowania i przechowywania danych w komputerze, umożliwiające efektywny dostęp i modyfikację.",
                Order = 1
            };

            context.DbContext.Steps.Add(emergencyStep);
            await context.DbContext.SaveChangesAsync();

            _logger.LogInformation("Utworzono awaryjny moduł, lekcję i krok.");
        }

        private async Task EnsureStackQueueHasStepsAsync(ContentContext context)
        {
            // Znajdź lekcję stack-queue
            var lesson = await context.DbContext.Lessons
                .FirstOrDefaultAsync(l => l.ExternalId == "stack-queue");

            if (lesson == null)
            {
                _logger.LogInformation("Lekcja stack-queue nie istnieje. Pomijam.");
                return;
            }

            // Sprawdź, czy lekcja ma kroki
            var stepsCount = await context.DbContext.Steps
                .Where(s => s.LessonId == lesson.Id)
                .CountAsync();

            if (stepsCount > 0)
            {
                _logger.LogInformation($"Lekcja stack-queue ma już {stepsCount} kroków. Pomijam.");
                return;
            }

            _logger.LogWarning("Lekcja stack-queue nie ma kroków. Dodaję awaryjne kroki.");

            // Dodaj kroki dla stack-queue
            var steps = new List<Step>
            {
                new Step
                {
                    LessonId = lesson.Id,
                    Type = "text",
                    Title = "Wprowadzenie do stosów i kolejek",
                    Content = "Stosy i kolejki to podstawowe struktury danych, które działają na zasadzie ograniczonego dostępu do elementów.",
                    Order = 1
                },
                new Step
                {
                    LessonId = lesson.Id,
                    Type = "text",
                    Title = "Stos (Stack)",
                    Content = "Stos to struktura danych działająca na zasadzie LIFO (Last In, First Out) - ostatni element dodany jest pierwszym, który zostanie pobrany.",
                    Order = 2
                },
                // ... więcej kroków
            };

            context.DbContext.Steps.AddRange(steps);
            await context.DbContext.SaveChangesAsync();

            _logger.LogInformation($"Dodano {steps.Count} awaryjnych kroków dla lekcji stack-queue.");
        }
    }
}