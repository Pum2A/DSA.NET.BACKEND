using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DSA.Core.Entities.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DSA.Infrastructure.Content.Sources
{
    public class JsonFileContentSource : IContentSource
    {
        private readonly string _baseDirectory;
        private readonly ILogger<JsonFileContentSource> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public JsonFileContentSource(string baseDirectory, ILogger<JsonFileContentSource> logger)
        {
            _baseDirectory = baseDirectory;
            _logger = logger;
        }

        public async Task LoadContentAsync(ContentContext context)
        {
            if (!Directory.Exists(_baseDirectory))
            {
                context.ValidationReport.AddIssue("JsonFile", $"Katalog {_baseDirectory} nie istnieje", ContentIssueSeverity.Error);
                return;
            }

            // Określ kolejność ładowania
            await LoadModulesAsync(context);
            await LoadLessonsAsync(context);
            await LoadStepsAsync(context);
        }

        private async Task<bool> LoadModulesAsync(ContentContext context)
        {
            var filePath = Path.Combine(_baseDirectory, "modules.json");
            if (!File.Exists(filePath)) return false;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var modules = DeserializeOrDefault<List<Module>>(json) ?? new List<Module>();

                // Inicjalizacja null-poprzednich pól
                foreach (var module in modules)
                    module.Prerequisites ??= new List<string>();

                if (modules.Count == 0) return false;

                // Aktualizacja/dodawanie modułów
                var existingModules = await context.DbContext.Modules.ToListAsync();
                var existingExternalIds = existingModules.Select(m => m.ExternalId).ToHashSet();

                foreach (var module in modules)
                {
                    if (string.IsNullOrEmpty(module.ExternalId)) continue;

                    if (existingExternalIds.Contains(module.ExternalId))
                    {
                        // Aktualizuj istniejący
                        var existing = existingModules.First(m => m.ExternalId == module.ExternalId);
                        UpdateModule(existing, module);
                    }
                    else
                    {
                        // Dodaj nowy
                        context.DbContext.Modules.Add(module);
                    }
                }

                await context.DbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ładowania modułów");
                context.ValidationReport.AddIssue("JsonFile", ex.Message, ContentIssueSeverity.Error);
                return false;
            }
        }

        private void UpdateModule(Module target, Module source)
        {
            target.Title = source.Title;
            target.Description = source.Description;
            target.Order = source.Order;
            target.Icon = source.Icon;
            target.IconColor = source.IconColor;
            target.Prerequisites = source.Prerequisites;
        }

        private async Task<bool> LoadLessonsAsync(ContentContext context)
        {
            var filePath = Path.Combine(_baseDirectory, "lessons.json");
            if (!File.Exists(filePath)) return false;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var lessons = DeserializeOrDefault<List<Lesson>>(json) ?? new List<Lesson>();

                // Inicjalizacja null-owych pól
                foreach (var lesson in lessons)
                    lesson.RequiredSkills ??= new List<string>();

                if (lessons.Count == 0) return false;

                // Pobierz moduły dla walidacji
                var modules = await context.DbContext.Modules.Select(m => m.Id).ToListAsync();

                // Aktualizacja/dodawanie lekcji
                var existingLessons = await context.DbContext.Lessons.ToListAsync();
                var existingExternalIds = existingLessons.Select(l => l.ExternalId).ToHashSet();

                foreach (var lesson in lessons)
                {
                    if (string.IsNullOrEmpty(lesson.ExternalId) || !modules.Contains(lesson.ModuleId))
                        continue;

                    if (existingExternalIds.Contains(lesson.ExternalId))
                    {
                        // Aktualizuj istniejącą
                        var existing = existingLessons.First(l => l.ExternalId == lesson.ExternalId);
                        UpdateLesson(existing, lesson);
                    }
                    else
                    {
                        // Dodaj nową
                        context.DbContext.Lessons.Add(lesson);
                    }
                }

                await context.DbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ładowania lekcji");
                context.ValidationReport.AddIssue("JsonFile", ex.Message, ContentIssueSeverity.Error);
                return false;
            }
        }

        private void UpdateLesson(Lesson target, Lesson source)
        {
            target.Title = source.Title;
            target.Description = source.Description;
            target.ModuleId = source.ModuleId;
            target.EstimatedTime = source.EstimatedTime;
            target.XpReward = source.XpReward;
            target.RequiredSkills = source.RequiredSkills;
        }

        private async Task<bool> LoadStepsAsync(ContentContext context)
        {
            var filePath = Path.Combine(_baseDirectory, "steps.json");
            if (!File.Exists(filePath)) return false;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var steps = DeserializeOrDefault<List<Step>>(json);

                if (steps == null || steps.Count == 0) return false;

                // Pobierz lekcje dla walidacji
                var validLessonIds = await context.DbContext.Lessons.Select(l => l.Id).ToListAsync();

                // Znajdź lekcję bubble-sort dla auto-mapowania
                var bubbleSortLesson = await context.DbContext.Lessons
                    .FirstOrDefaultAsync(l => l.ExternalId == "bubble-sort");

                // Aktualizacja/dodawanie kroków
                var existingSteps = await context.DbContext.Steps.ToListAsync();
                var existingStepIds = existingSteps.Select(s => s.Id).ToHashSet();

                foreach (var step in steps)
                {
                    // Auto-mapowanie dla bubble-sort
                    if (bubbleSortLesson != null && step.LessonId == 1)
                        step.LessonId = bubbleSortLesson.Id;

                    if (!validLessonIds.Contains(step.LessonId)) continue;

                    if (existingStepIds.Contains(step.Id))
                    {
                        // Aktualizuj istniejący krok
                        var existing = existingSteps.First(s => s.Id == step.Id);
                        UpdateStep(existing, step);
                    }
                    else
                    {
                        // Dodaj nowy krok
                        context.DbContext.Steps.Add(step);
                    }
                }

                await context.DbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd ładowania kroków");
                context.ValidationReport.AddIssue("JsonFile", ex.Message, ContentIssueSeverity.Error);
                return false;
            }
        }

        private void UpdateStep(Step target, Step source)
        {
            target.Type = source.Type;
            target.Title = source.Title;
            target.Content = source.Content;
            target.Code = source.Code;
            target.Language = source.Language;
            target.ImageUrl = source.ImageUrl;
            target.Order = source.Order;
            target.AdditionalData = source.AdditionalData;
        }

        private T DeserializeOrDefault<T>(string json) where T : class
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch
            {
                return default;
            }
        }
    }
}