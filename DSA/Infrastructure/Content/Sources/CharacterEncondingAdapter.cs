using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DSA.Infrastructure.Content.Sources
{
    public class CharacterEncodingAdapter : IContentSource
    {
        private readonly ILogger<CharacterEncodingAdapter> _logger;
        private readonly Dictionary<char, string> _polishChars = new()
        {
            {'ą', "a"}, {'ć', "c"}, {'ę', "e"}, {'ł', "l"}, {'ń', "n"},
            {'ó', "o"}, {'ś', "s"}, {'ź', "z"}, {'ż', "z"},
            {'Ą', "A"}, {'Ć', "C"}, {'Ę', "E"}, {'Ł', "L"}, {'Ń', "N"},
            {'Ó', "O"}, {'Ś', "S"}, {'Ź', "Z"}, {'Ż', "Z"}
        };

        public CharacterEncodingAdapter(ILogger<CharacterEncodingAdapter> logger) => _logger = logger;

        public async Task LoadContentAsync(ContentContext context)
        {
            try
            {
                // Konwersja znaków w modułach
                var modules = await context.DbContext.Modules.ToListAsync();
                foreach (var module in modules)
                {
                    module.Title = ConvertPolishChars(module.Title);
                    module.Description = ConvertPolishChars(module.Description);

                    if (module.Prerequisites != null)
                        for (int i = 0; i < module.Prerequisites.Count; i++)
                            module.Prerequisites[i] = ConvertPolishChars(module.Prerequisites[i]);
                }

                // Konwersja znaków w lekcjach
                var lessons = await context.DbContext.Lessons.ToListAsync();
                foreach (var lesson in lessons)
                {
                    lesson.Title = ConvertPolishChars(lesson.Title);
                    lesson.Description = ConvertPolishChars(lesson.Description);
                    lesson.EstimatedTime = ConvertPolishChars(lesson.EstimatedTime);

                    if (lesson.RequiredSkills != null)
                        for (int i = 0; i < lesson.RequiredSkills.Count; i++)
                            lesson.RequiredSkills[i] = ConvertPolishChars(lesson.RequiredSkills[i]);
                }

                // Konwersja znaków w krokach
                var steps = await context.DbContext.Steps.ToListAsync();
                foreach (var step in steps)
                {
                    step.Title = ConvertPolishChars(step.Title);
                    step.Content = ConvertPolishChars(step.Content);

                    if (!string.IsNullOrEmpty(step.AdditionalData))
                    {
                        switch (step.Type.ToLower())
                        {
                            case "quiz":
                                step.AdditionalData = ReplaceInJson(step.AdditionalData, "question");
                                step.AdditionalData = ReplaceInJson(step.AdditionalData, "explanation");
                                step.AdditionalData = ReplaceInJson(step.AdditionalData, "text");
                                break;
                            case "interactive":
                            case "coding":
                            case "challenge":
                                step.AdditionalData = ReplaceInJson(step.AdditionalData, "hint");
                                break;
                        }
                    }
                }

                // Zapisz zmiany
                await context.DbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd konwersji znaków");
                context.ValidationReport.AddIssue("CharacterEncoding", ex.Message, ContentIssueSeverity.Error);
            }
        }

        private string ConvertPolishChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var result = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (_polishChars.TryGetValue(c, out string replacement))
                    result.Append(replacement);
                else
                    result.Append(c);
            }

            return result.ToString();
        }

        private string ReplaceInJson(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return json;

            try
            {
                var fieldPattern = $"\"{fieldName}\":\\s*\"([^\"]+)\"";
                return Regex.Replace(json, fieldPattern, match =>
                {
                    var value = match.Groups[1].Value;
                    var convertedValue = ConvertPolishChars(value);
                    return $"\"{fieldName}\":\"{convertedValue}\"";
                });
            }
            catch
            {
                return json;
            }
        }
    }
}