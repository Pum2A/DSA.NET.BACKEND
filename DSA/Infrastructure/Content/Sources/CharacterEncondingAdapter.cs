using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSA.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DSA.Infrastructure.Content.Sources
{
    /// <summary>
    /// Adapter konwersji znaków specjalnych (w tym polskich) do znaków ASCII.
    /// Pozwala rozwiązać problemy z kodowaniem znaków w systemach, które nie obsługują poprawnie UTF-8.
    /// </summary>
    public class CharacterEncodingAdapter : IContentSource
    {
        private readonly ILogger<CharacterEncodingAdapter> _logger;

        public CharacterEncodingAdapter(ILogger<CharacterEncodingAdapter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Konwertuje polskie znaki we wszystkich encjach (modułach, lekcjach, krokach)
        /// </summary>
        public async Task LoadContentAsync(ContentContext context)
        {
            _logger.LogInformation("Uruchamiam adapter konwersji znaków specjalnych");

            try
            {
                // Konwersja znaków w modułach
                var modules = await context.DbContext.Modules.ToListAsync();
                foreach (var module in modules)
                {
                    module.Title = ConvertPolishChars(module.Title);
                    module.Description = ConvertPolishChars(module.Description);
                }
                _logger.LogInformation($"Przekonwertowano znaki w {modules.Count} modułach");

                // Konwersja znaków w lekcjach
                var lessons = await context.DbContext.Lessons.ToListAsync();
                foreach (var lesson in lessons)
                {
                    lesson.Title = ConvertPolishChars(lesson.Title);
                    lesson.Description = ConvertPolishChars(lesson.Description);
                    if (!string.IsNullOrEmpty(lesson.EstimatedTime))
                    {
                        lesson.EstimatedTime = ConvertPolishChars(lesson.EstimatedTime);
                    }
                }
                _logger.LogInformation($"Przekonwertowano znaki w {lessons.Count} lekcjach");

                // Konwersja znaków w krokach
                var steps = await context.DbContext.Steps.ToListAsync();
                foreach (var step in steps)
                {
                    step.Title = ConvertPolishChars(step.Title);
                    step.Content = ConvertPolishChars(step.Content);

                    // Obsługa pól specyficznych dla różnych typów kroków
                    if (step.Type == "code" && !string.IsNullOrEmpty(step.Code))
                    {
                        // W kodzie nie konwertujemy znaków, bo może to zepsuć kod
                        // step.Code = ConvertPolishChars(step.Code);

                        // Ale komentarze możemy konwertować, choć to bardziej zaawansowane
                        // i wymagałoby analizy składni kodu
                    }

                    if (!string.IsNullOrEmpty(step.AdditionalData))
                    {
                        // Konwersja w zależności od typu kroku
                        switch (step.Type)
                        {
                            case "quiz":
                                // Konwertuj pola w JSON dla quizów
                                step.AdditionalData = ReplaceInJson(step.AdditionalData, "question");
                                step.AdditionalData = ReplaceInJson(step.AdditionalData, "explanation");
                                step.AdditionalData = ReplaceInJson(step.AdditionalData, "text");
                                break;

                            case "interactive":
                                // Konwertuj pola w JSON dla interaktywnych kroków
                                step.AdditionalData = ReplaceInJson(step.AdditionalData, "hint");
                                step.AdditionalData = ReplaceInJson(step.AdditionalData, "description");
                                break;
                        }
                    }
                }
                _logger.LogInformation($"Przekonwertowano znaki w {steps.Count} krokach");

                // Zapisz zmiany
                await context.DbContext.SaveChangesAsync();
                _logger.LogInformation("Pomyślnie przekonwertowano wszystkie znaki specjalne");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas konwersji znaków specjalnych");
                context.ValidationReport.AddIssue("CharacterEncoding", ex.Message, ContentIssueSeverity.Error);
            }
        }


        private string ConvertPolishChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Mapowanie polskich znaków na odpowiedniki ASCII
            var polishChars = new Dictionary<char, string>
            {
                {'ą', "a"}, {'ć', "c"}, {'ę', "e"}, {'ł', "l"}, {'ń', "n"},
                {'ó', "o"}, {'ś', "s"}, {'ź', "z"}, {'ż', "z"},
                {'Ą', "A"}, {'Ć', "C"}, {'Ę', "E"}, {'Ł', "L"}, {'Ń', "N"},
                {'Ó', "O"}, {'Ś', "S"}, {'Ź', "Z"}, {'Ż', "Z"}
            };

            // Użyj StringBuilder dla wydajności
            var result = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (polishChars.TryGetValue(c, out string replacement))
                {
                    result.Append(replacement);
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }


        private string ReplaceInJson(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return json;

            try
            {
                // Wzór regex do znalezienia pola w JSON
                var fieldPattern = $"\"{fieldName}\":\\s*\"([^\"]+)\"";

                return Regex.Replace(json, fieldPattern, match =>
                {
                    var value = match.Groups[1].Value;
                    var convertedValue = ConvertPolishChars(value);
                    return $"\"{fieldName}\":\"{convertedValue}\"";
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Błąd podczas konwersji pola {fieldName} w JSON");
                return json; // W przypadku błędu zwróć oryginalny JSON
            }
        }
    }
}