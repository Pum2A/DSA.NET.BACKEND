using DSA.Core.DTOs.Lessons;
using DSA.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace DSA.Core.Extensions
{
    public static class MappingExtensions
    {
        // Mapowanie Module -> ModuleDto
        public static ModuleDto ToDto(this Module module)
        {
            if (module == null) return null;

            return new ModuleDto
            {
                Id = module.Id,
                ExternalId = module.ExternalId,
                Title = module.Title,
                Description = module.Description,
                Order = module.Order,
                Icon = module.Icon,
                IconColor = module.IconColor,
                Lessons = module.Lessons?.Select(l => l.ToDto()).ToList()
            };
        }

        // Mapowanie IEnumerable<Module> -> IEnumerable<ModuleDto>
        public static IEnumerable<ModuleDto> ToDto(this IEnumerable<Module> modules)
        {
            return modules?.Select(m => m.ToDto()).ToList();
        }

        // Mapowanie Lesson -> LessonDto
        public static LessonDto ToDto(this Lesson lesson)
        {
            if (lesson == null) return null;

            return new LessonDto
            {
                Id = lesson.Id,
                ExternalId = lesson.ExternalId,
                Title = lesson.Title,
                Description = lesson.Description,
                EstimatedTime = lesson.EstimatedTime,
                XpReward = lesson.XpReward,
                ModuleId = lesson.ModuleId,
                Steps = lesson.Steps?.Select(s => s.ToDto()).ToList()
            };
        }

        // Mapowanie Step -> StepDto
        public static StepDto ToDto(this Step step)
        {
            if (step == null) return null;

            var dto = new StepDto
            {
                Id = step.Id,
                Type = step.Type,
                Title = step.Title,
                Content = step.Content,
                Code = step.Code,
                Language = step.Language,
                ImageUrl = step.ImageUrl,
                Order = step.Order,
                LessonId = step.LessonId
            };

            // Mapowanie danych dodatkowych
            if (!string.IsNullOrEmpty(step.AdditionalData))
            {
                try
                {
                    if (step.Type == "quiz")
                    {
                        dto.Options = JsonSerializer.Deserialize<List<QuizOptionDto>>(step.AdditionalData);
                    }
                    else if (step.Type == "interactive" || step.Type == "challenge")
                    {
                        dto.TestCases = JsonSerializer.Deserialize<List<TestCaseDto>>(step.AdditionalData);
                    }
                    else if (step.Type == "list")
                    {
                        dto.Items = JsonSerializer.Deserialize<List<ListItemDto>>(step.AdditionalData);
                    }
                }
                catch (Exception)
                {
                    // Ignoruj błędy deserializacji
                }
            }

            return dto;
        }

        // Mapowanie UserProgress -> UserProgressDto
        public static UserProgressDto ToDto(this UserProgress progress)
        {
            if (progress == null) return null;

            return new UserProgressDto
            {
                Id = progress.Id,
                UserId = progress.UserId,
                LessonId = progress.LessonId,
                IsCompleted = progress.IsCompleted,
                StartedAt = (DateTime)progress.StartedAt,
                CompletedAt = progress.CompletedAt,
                CurrentStepIndex = progress.CurrentStepIndex,
                XpEarned = progress.XpEarned
            };
        }
    }
}