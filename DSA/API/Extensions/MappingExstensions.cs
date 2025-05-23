using DSA.Core.DTOs.Lessons.Interactive;
using DSA.Core.DTOs.Lessons.Learning;
using DSA.Core.DTOs.Lessons.Quiz;
using DSA.Core.Entities.Learning;
using DSA.Core.Entities.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace DSA.Core.Extensions
{
    public static class MappingExtensions
    {
        // Module -> ModuleDto
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
                Prerequisites = module.Prerequisites ?? new List<string>(),
                Lessons = module.Lessons?.Select(l => l.ToDto()).ToList() ?? new List<LessonDto>()
            };
        }

        // IEnumerable<Module> -> IEnumerable<ModuleDto>
        public static IEnumerable<ModuleDto> ToDto(this IEnumerable<Module> modules)
        {
            return modules?.Select(m => m.ToDto()).ToList() ?? new List<ModuleDto>();
        }

        // Lesson -> LessonDto
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
                RequiredSkills = lesson.RequiredSkills ?? new List<string>(),
                Steps = lesson.Steps?.OrderBy(s => s.Order).Select(s => s.ToDto()).ToList() ?? new List<StepDto>()
            };
        }

        // Step -> StepDto
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
                LessonId = step.LessonId,
                AdditionalData = step.AdditionalData
            };

            // Proces tylko jeśli mamy dane
            if (!string.IsNullOrEmpty(step.AdditionalData))
            {
                try
                {
                    switch (step.Type.ToLower())
                    {
                        case "quiz":
                            var quiz = step.GetTypedData<QuizData>();
                            if (quiz != null)
                            {
                                dto.Question = quiz.Question;
                                dto.Options = quiz.Options;
                                dto.CorrectAnswer = quiz.CorrectAnswer;
                                dto.Explanation = quiz.Explanation;
                                dto.QuizData = quiz;
                            }
                            break;

                        case "interactive":
                            var interactive = step.GetTypedData<InteractiveData>();
                            if (interactive != null)
                            {
                                dto.Items = interactive.Items;
                                dto.Hint = interactive.TaskDescription;
                                dto.InteractiveData = interactive;
                            }
                            break;

                        case "coding":
                        case "challenge":
                            var coding = step.GetTypedData<CodingData>();
                            if (coding != null)
                            {
                                dto.InitialCode = coding.InitialCode;
                                dto.TestCases = coding.TestCases;
                                dto.Hint = coding.Hint;
                                dto.Language = coding.Language;

                                // Konwersja z CodingData na ChallengeData
                                dto.ChallengeData = new ChallengeData
                                {
                                    InitialCode = coding.InitialCode,
                                    TestCases = coding.TestCases,
                                    Hint = coding.Hint,
                                    Language = coding.Language,
                                    Solution = coding.Solution
                                };
                            }
                            break;

                        case "video":
                            var videoData = step.GetTypedData<VideoData>();
                            if (videoData != null)
                            {
                                dto.VideoUrl = videoData.Url;
                                dto.Duration = videoData.Duration;
                                dto.RequireFullWatch = videoData.RequireFullWatch;
                                dto.VideoData = videoData;
                            }
                            break;

                        case "list":
                            var listItems = step.GetTypedData<List<ListItemDto>>();
                            if (listItems != null)
                            {
                                dto.Items = listItems;
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing AdditionalData for step {step.Id}: {ex.Message}");
                }
            }

            return dto;
        }

        // UserProgress -> UserProgressDto
        public static UserProgressDto ToDto(this UserProgress progress)
        {
            if (progress == null) return null;

            return new UserProgressDto
            {
                Id = progress.Id,
                UserId = progress.UserId,
                LessonId = progress.LessonId,
                IsCompleted = progress.IsCompleted,
                StartedAt = progress.StartedAt ?? DateTime.MinValue,
                CompletedAt = progress.CompletedAt,
                CurrentStepIndex = progress.CurrentStepIndex,
                XpEarned = progress.XpEarned,
                LastUpdated = progress.LastUpdated
            };
        }
    }
}