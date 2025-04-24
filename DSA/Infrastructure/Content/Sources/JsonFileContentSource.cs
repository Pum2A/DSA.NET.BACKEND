using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DSA.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DSA.Infrastructure.Content.Sources
{
    public class JsonFileContentSource : IContentSource
    {
        private readonly string _baseDirectory;
        private readonly ILogger<JsonFileContentSource> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonFileContentSource(string baseDirectory, ILogger<JsonFileContentSource> logger)
        {
            _baseDirectory = baseDirectory;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
        }

        public async Task LoadContentAsync(ContentContext context)
        {
            _logger.LogInformation($"Loading content from individual JSON files in {_baseDirectory}");

            if (!Directory.Exists(_baseDirectory))
            {
                _logger.LogError($"Directory does not exist: {_baseDirectory}");
                context.ValidationReport.AddIssue("JsonFile", $"Katalog {_baseDirectory} nie istnieje", ContentIssueSeverity.Error);
                return;
            }

            // Określ kolejność ładowania
            var modulesSuccess = await LoadModulesAsync(context);
            var lessonsSuccess = await LoadLessonsAsync(context);
            var stepsSuccess = await LoadStepsAsync(context);

            var modulesPath = Path.Combine(_baseDirectory, "modules.json");
            var lessonsPath = Path.Combine(_baseDirectory, "lessons.json");
            var stepsPath = Path.Combine(_baseDirectory, "steps.json");

            if (!modulesSuccess && !lessonsSuccess && !stepsSuccess)
            {
                _logger.LogWarning("No valid JSON files found to load data from!");
                context.ValidationReport.AddIssue("JsonFile", "Nie znaleziono żadnych poprawnych plików JSON do załadowania", ContentIssueSeverity.Warning);
            }
        }

        private async Task<bool> LoadModulesAsync(ContentContext context)
        {
            var filePath = Path.Combine(_baseDirectory, "modules.json");
            if (!File.Exists(filePath))
            {
                _logger.LogInformation($"Modules file not found: {filePath}");
                return false;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                _logger.LogInformation($"Reading modules.json, size: {json.Length} bytes");

                List<Module> modules;
                try
                {
                    modules = JsonSerializer.Deserialize<List<Module>>(json, _jsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse modules.json");
                    context.ValidationReport.AddIssue("JsonFile", $"Błąd parsowania modules.json: {ex.Message}", ContentIssueSeverity.Error);
                    return false;
                }

                if (modules == null || modules.Count == 0)
                {
                    _logger.LogWarning("Plik modules.json nie zawiera żadnych modułów");
                    context.ValidationReport.AddIssue("JsonFile", "Plik modules.json nie zawiera żadnych modułów", ContentIssueSeverity.Warning);
                    return false;
                }

                // Sprawdź, czy moduły już istnieją
                var existingModules = await context.DbContext.Modules.ToListAsync();
                var existingExternalIds = existingModules.Select(m => m.ExternalId).ToHashSet();

                int added = 0;
                int updated = 0;

                foreach (var module in modules)
                {
                    // Walidacja
                    if (string.IsNullOrEmpty(module.ExternalId))
                    {
                        context.ValidationReport.AddIssue("JsonFile", $"Moduł bez ExternalId: {module.Title}", ContentIssueSeverity.Warning);
                        continue;
                    }

                    if (existingExternalIds.Contains(module.ExternalId))
                    {
                        // Aktualizuj istniejący moduł
                        var existingModule = existingModules.First(m => m.ExternalId == module.ExternalId);
                        existingModule.Title = module.Title;
                        existingModule.Description = module.Description;
                        existingModule.Order = module.Order;
                        existingModule.Icon = module.Icon;
                        existingModule.IconColor = module.IconColor;
                        updated++;
                    }
                    else
                    {
                        // Dodaj nowy moduł
                        context.DbContext.Modules.Add(module);
                        added++;
                    }
                }

                await context.DbContext.SaveChangesAsync();
                _logger.LogInformation($"Załadowano moduły: {added} dodanych, {updated} zaktualizowanych");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas ładowania modułów: {ex.Message}");
                context.ValidationReport.AddIssue("JsonFile", $"Błąd podczas ładowania modułów: {ex.Message}", ContentIssueSeverity.Error);
                return false;
            }
        }

        private async Task<bool> LoadLessonsAsync(ContentContext context)
        {
            var filePath = Path.Combine(_baseDirectory, "lessons.json");
            if (!File.Exists(filePath))
            {
                _logger.LogInformation($"Lessons file not found: {filePath}");
                return false;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                _logger.LogInformation($"Reading lessons.json, size: {json.Length} bytes");

                List<Lesson> lessons;
                try
                {
                    lessons = JsonSerializer.Deserialize<List<Lesson>>(json, _jsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse lessons.json");
                    context.ValidationReport.AddIssue("JsonFile", $"Błąd parsowania lessons.json: {ex.Message}", ContentIssueSeverity.Error);
                    return false;
                }

                if (lessons == null || lessons.Count == 0)
                {
                    _logger.LogWarning("Plik lessons.json nie zawiera żadnych lekcji");
                    context.ValidationReport.AddIssue("JsonFile", "Plik lessons.json nie zawiera żadnych lekcji", ContentIssueSeverity.Warning);
                    return false;
                }

                // Pobierz moduły dla mapowania
                var modules = await context.DbContext.Modules.ToListAsync();

                // Pobierz istniejące lekcje
                var existingLessons = await context.DbContext.Lessons.ToListAsync();
                var existingExternalIds = existingLessons.Select(l => l.ExternalId).ToHashSet();

                int added = 0;
                int updated = 0;
                int skipped = 0;

                foreach (var lesson in lessons)
                {
                    // Walidacja
                    if (string.IsNullOrEmpty(lesson.ExternalId))
                    {
                        context.ValidationReport.AddIssue("JsonFile", $"Lekcja bez ExternalId: {lesson.Title}", ContentIssueSeverity.Warning);
                        skipped++;
                        continue;
                    }

                    // Nie używamy ModuleExternalId, ale sprawdzamy czy ModuleId istnieje
                    if (lesson.ModuleId <= 0 || !modules.Any(m => m.Id == lesson.ModuleId))
                    {
                        context.ValidationReport.AddIssue("JsonFile", $"Lekcja '{lesson.Title}' odwołuje się do nieistniejącego modułu: ID={lesson.ModuleId}", ContentIssueSeverity.Warning);
                        skipped++;
                        continue;
                    }

                    if (existingExternalIds.Contains(lesson.ExternalId))
                    {
                        // Aktualizuj istniejącą lekcję
                        var existingLesson = existingLessons.First(l => l.ExternalId == lesson.ExternalId);
                        existingLesson.Title = lesson.Title;
                        existingLesson.Description = lesson.Description;
                        existingLesson.ModuleId = lesson.ModuleId;
                        existingLesson.EstimatedTime = lesson.EstimatedTime;
                        existingLesson.XpReward = lesson.XpReward;
                        updated++;
                    }
                    else
                    {
                        // Dodaj nową lekcję
                        context.DbContext.Lessons.Add(lesson);
                        added++;
                    }
                }

                await context.DbContext.SaveChangesAsync();
                _logger.LogInformation($"Załadowano lekcje: {added} dodanych, {updated} zaktualizowanych, {skipped} pominiętych");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas ładowania lekcji: {ex.Message}");
                context.ValidationReport.AddIssue("JsonFile", $"Błąd podczas ładowania lekcji: {ex.Message}", ContentIssueSeverity.Error);
                return false;
            }
        }

        private async Task<bool> LoadStepsAsync(ContentContext context)
        {
            var filePath = Path.Combine(_baseDirectory, "steps.json");
            if (!File.Exists(filePath))
            {
                _logger.LogInformation($"Steps file not found: {filePath}");
                return false;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                _logger.LogInformation($"Reading steps.json, size: {json.Length} bytes");

                List<Step> steps;
                try
                {
                    steps = JsonSerializer.Deserialize<List<Step>>(json, _jsonOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse steps.json");
                    context.ValidationReport.AddIssue("JsonFile", $"Błąd parsowania steps.json: {ex.Message}", ContentIssueSeverity.Error);
                    return false;
                }

                if (steps == null || steps.Count == 0)
                {
                    _logger.LogWarning("Plik steps.json nie zawiera żadnych kroków");
                    context.ValidationReport.AddIssue("JsonFile", "Plik steps.json nie zawiera żadnych kroków", ContentIssueSeverity.Warning);
                    return false;
                }

                // Pobierz wszystkie lekcje dla powiązania
                var lessons = await context.DbContext.Lessons.ToListAsync();
                var lessonIds = lessons.Select(l => l.Id).ToHashSet();

                // Pobierz istniejące kroki
                var existingSteps = await context.DbContext.Steps.ToListAsync();
                var existingStepIds = existingSteps.Select(s => s.Id).ToHashSet();

                int added = 0;
                int updated = 0;
                int skipped = 0;

                foreach (var step in steps)
                {
                    // Walidacja
                    if (!lessonIds.Contains(step.LessonId))
                    {
                        context.ValidationReport.AddIssue("JsonFile", $"Krok ID={step.Id} odwołuje się do nieistniejącej lekcji ID={step.LessonId}", ContentIssueSeverity.Warning);
                        skipped++;
                        continue;
                    }

                    if (existingStepIds.Contains(step.Id))
                    {
                        // Aktualizuj istniejący krok
                        var existingStep = existingSteps.First(s => s.Id == step.Id);
                        existingStep.Type = step.Type;
                        existingStep.Title = step.Title;
                        existingStep.Content = step.Content;
                        existingStep.Code = step.Code;
                        existingStep.Language = step.Language;
                        existingStep.ImageUrl = step.ImageUrl;
                        existingStep.Order = step.Order;
                        existingStep.AdditionalData = step.AdditionalData;
                        updated++;
                    }
                    else
                    {
                        // Dodaj nowy krok
                        context.DbContext.Steps.Add(step);
                        added++;
                    }
                }

                await context.DbContext.SaveChangesAsync();
                _logger.LogInformation($"Załadowano kroki: {added} dodanych, {updated} zaktualizowanych, {skipped} pominiętych");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas ładowania kroków: {ex.Message}");
                context.ValidationReport.AddIssue("JsonFile", $"Błąd podczas ładowania kroków: {ex.Message}", ContentIssueSeverity.Error);
                return false;
            }
        }
    }
}