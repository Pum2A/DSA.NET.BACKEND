using DSA.Core.DTOs.Lessons.Interactive;
using DSA.Core.DTOs.Lessons.Learning;
using DSA.Core.DTOs.Lessons.Quiz;
using DSA.Core.Entities.Learning;
using DSA.Core.Entities.User;
using DSA.Core.Enums;
using DSA.Core.Extensions;
using DSA.Core.Interfaces;
using DSA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace DSA.Infrastructure.Services
{
    public class LessonService : ILessonService
    {
        private readonly ILessonRepository _lessonRepo;
        private readonly IModuleRepository _moduleRepo;
        private readonly IUserProgressRepository _progressRepo;
        private readonly ApplicationDbContext _context;
        private readonly IUserActivityService _activityService;
        private readonly INotificationService _notifyService;

        // Stałe konfiguracyjne
        private const int BASE_XP = 5;
        private const int BASE_LEVEL_XP = 100;
        private const double LEVEL_MULTIPLIER = 1.5;

        public LessonService(
            ILessonRepository lessonRepository,
            IModuleRepository moduleRepository,
            IUserProgressRepository userProgressRepository,
            ApplicationDbContext context,
            IUserActivityService userActivityService,
            INotificationService notificationService)
        {
            _lessonRepo = lessonRepository;
            _moduleRepo = moduleRepository;
            _progressRepo = userProgressRepository;
            _context = context;
            _activityService = userActivityService;
            _notifyService = notificationService;
        }

        public async Task<IEnumerable<ModuleDto>> GetAllModulesAsync()
        {
            var modules = (await _moduleRepo.GetAllAsync()).ToList();

            // Pobierz lekcje dla modułów w jednym zapytaniu
            var moduleIds = modules.Select(m => m.Id).ToList();
            var moduleWithLessons = await Task.WhenAll(
                moduleIds.Select(id => _moduleRepo.GetModuleWithLessonsAsync(id))
            );

            // Przypisz lekcje do odpowiednich modułów
            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].Lessons = moduleWithLessons[i].Lessons ?? new List<Lesson>();
            }

            // Konwertuj na DTO i oblicz dodatkowe dane
            var moduleDtos = modules.ToDto();
            foreach (var dto in moduleDtos)
            {
                dto.TotalXP = dto.Lessons?.Sum(l => l.XpReward) ?? 0;
                dto.EstimatedTotalTime = CalculateTotalTime(dto.Lessons);
                dto.Difficulty = CalculateModuleDifficulty(dto.Lessons);
            }

            return moduleDtos;
        }

        public async Task<ModuleDto> GetModuleByIdAsync(string moduleId)
        {
            var module = await _moduleRepo.GetModuleWithLessonsByExternalIdAsync(moduleId);
            if (module == null) return null;

            var moduleDto = module.ToDto();
            moduleDto.TotalXP = moduleDto.Lessons?.Sum(l => l.XpReward) ?? 0;
            moduleDto.EstimatedTotalTime = CalculateTotalTime(moduleDto.Lessons);
            moduleDto.Difficulty = CalculateModuleDifficulty(moduleDto.Lessons);

            return moduleDto;
        }

        public async Task<LessonDto> GetLessonByIdAsync(string lessonId)
        {
            var lesson = await _lessonRepo.GetLessonWithStepsByExternalIdAsync(lessonId);
            if (lesson == null) return null;

            var lessonDto = lesson.ToDto();
            foreach (var step in lessonDto.Steps)
            {
                ProcessStepAdditionalData(step);
            }

            return lessonDto;
        }

        public async Task<LessonProgressDto> GetLessonProgressAsync(string userId, string lessonId)
        {
            var lesson = await _lessonRepo.GetLessonWithStepsByExternalIdAsync(lessonId);
            if (lesson == null) return null;

            var progress = await _progressRepo.GetUserProgressAsync(userId, lesson.Id);
            var stepsCount = lesson.Steps.Count;

            return new LessonProgressDto
            {
                LessonId = lessonId,
                CompletedSteps = progress?.CurrentStepIndex ?? 0,
                TotalSteps = stepsCount,
                IsCompleted = progress?.IsCompleted ?? false,
                LastActivityDate = progress?.LastUpdated,
                CompletionPercentage = stepsCount > 0 ? (progress?.CurrentStepIndex ?? 0) * 100.0 / stepsCount : 0,
                EarnedXP = progress?.XpEarned ?? 0
            };
        }

        public async Task<ModuleProgressDto> GetModuleProgressAsync(string userId, string moduleId)
        {
            var module = await _moduleRepo.GetModuleWithLessonsByExternalIdAsync(moduleId);
            if (module == null) return null;

            var allProgress = await _progressRepo.GetUserProgressForModuleAsync(userId, module.Id);
            var totalLessons = module.Lessons.Count;

            return new ModuleProgressDto
            {
                TotalLessons = totalLessons,
                CompletedLessons = allProgress.Count(p => p.IsCompleted),
                InProgressLessons = allProgress.Count(p => !p.IsCompleted && p.StartedAt.HasValue),
                TotalXPEarned = allProgress.Sum(p => p.XpEarned),
                LastActivity = allProgress.Any() ? allProgress.Max(p => p.LastUpdated) : null,
                CompletionPercentage = totalLessons > 0
                    ? Math.Round(100.0 * allProgress.Count(p => p.IsCompleted) / totalLessons, 2)
                    : 0
            };
        }

        public async Task<bool> CompleteStepAsync(string userId, string lessonId, int stepIndex, StepCompletionData completionData)
        {
            try
            {
                var lesson = await _lessonRepo.GetLessonWithStepsByExternalIdAsync(lessonId);
                if (lesson == null) return false;

                var step = lesson.Steps.FirstOrDefault(s => s.Order == stepIndex);
                if (step == null) return false;

                var validationResult = await ValidateStepCompletion(step, completionData);
                if (!validationResult.IsValid) return false;

                var now = DateTime.UtcNow;
                var stepXp = CalculateStepXP(step, completionData);
                var progress = await _progressRepo.GetUserProgressAsync(userId, lesson.Id);

                if (progress == null)
                {
                    // Tworzenie nowej progresji
                    progress = new UserProgress
                    {
                        UserId = userId,
                        LessonId = lesson.Id,
                        IsCompleted = false, // Explicit initialization for clarity
                        CurrentStepIndex = stepIndex + 1,
                        StartedAt = now,
                        LastUpdated = now,
                        XpEarned = stepXp
                    };
                    await _progressRepo.AddAsync(progress);
                }
                else
                {
                    // Aktualizacja istniejącej
                    progress.CurrentStepIndex = stepIndex + 1;
                    progress.LastUpdated = now;
                    progress.XpEarned += stepXp;
                    await _progressRepo.UpdateAsync(progress);
                }

                // Logowanie aktywności
                await _activityService.LogActivityAsync(
                    userId,
                    UserActionType.StepCompleted,
                    $"{lessonId}:{stepIndex}",
                    JsonSerializer.Serialize(completionData)
                );

                // Sprawdzenie ukończenia lekcji
                if (stepIndex == lesson.Steps.Count - 1)
                {
                    await CompleteLessonAsync(userId, lessonId);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CompleteLessonAsync(string userId, string lessonId)
        {
            var lesson = await _lessonRepo.GetByExternalIdAsync(lessonId);
            if (lesson == null) return false;

            var progress = await _progressRepo.GetUserProgressAsync(userId, lesson.Id);
            var now = DateTime.UtcNow;
            var isFirstCompletion = false;

            if (progress == null)
            {
                // Nowa progresja
                progress = new UserProgress
                {
                    UserId = userId,
                    LessonId = lesson.Id,
                    IsCompleted = true,
                    CurrentStepIndex = 0,
                    StartedAt = now,
                    CompletedAt = now,
                    LastUpdated = now,
                    XpEarned = lesson.XpReward
                };
                await _progressRepo.AddAsync(progress);
                isFirstCompletion = true;
            }
            else if (!progress.IsCompleted)
            {
                // Aktualizacja istniejącej
                progress.IsCompleted = true;
                progress.CompletedAt = now;
                progress.LastUpdated = now;
                progress.XpEarned = lesson.XpReward;
                await _progressRepo.UpdateAsync(progress);
                isFirstCompletion = true;
            }

            if (isFirstCompletion)
            {
                await UpdateUserXpAsync(userId, lesson.XpReward);
                await _activityService.LogActivityAsync(userId, UserActionType.LessonCompleted, lessonId);
                await CheckAndAwardAchievements(userId, lesson);
            }

            return true;
        }

        public async Task<UserLearningStatsDto> GetUserLearningStatsAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.UserProgresses)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            return new UserLearningStatsDto
            {
                TotalLessonsCompleted = user.UserProgresses.Count(p => p.IsCompleted),
                TotalXPEarned = user.ExperiencePoints,
                CurrentLevel = user.Level,
                XPToNextLevel = CalculateXPToNextLevel(user.ExperiencePoints, user.Level),
                AverageCompletionRate = await CalculateAverageCompletionRate(userId),
                TotalLearningTime = CalculateTotalLearningTime(user.UserProgresses),
                CompletedLessonsByType = await GetCompletedLessonsByType(userId)
            };
        }

        public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(string userId, int count = 10)
        {
            var activities = await _activityService.GetUserActivitiesAsync(userId, count);

            return activities.Select(a => new RecentActivityDto
            {
                ActivityType = a.ActionType.ToString(),
                Description = a.AdditionalInfo,
                Timestamp = a.ActionTime,
                XPEarned = CalculateActivityXP(a.ActionType),
                RelatedContent = a.ReferenceId
            }).ToList();
        }

        public async Task<Dictionary<string, double>> GetModuleCompletionRatesAsync(string userId)
        {
            var modules = await _moduleRepo.GetAllAsync();

            // Użyj Task.WhenAll dla równoległego przetwarzania
            var tasks = modules.Select(async module => {
                var progress = await GetModuleProgressAsync(userId, module.ExternalId);
                return (module.ExternalId, progress.CompletionPercentage);
            });

            var results = await Task.WhenAll(tasks);
            return results.ToDictionary(r => r.ExternalId, r => r.CompletionPercentage);
        }

        public async Task<StepVerificationResult> VerifyStepAnswerAsync(string userId, string lessonId, int stepIndex, object answer)
        {
            try
            {
                var lesson = await _lessonRepo.GetLessonWithStepsByExternalIdAsync(lessonId);
                if (lesson == null)
                    return new StepVerificationResult { IsCorrect = false, Feedback = "Lekcja nie znaleziona" };

                var step = lesson.Steps.FirstOrDefault(s => s.Order == stepIndex);
                if (step == null)
                    return new StepVerificationResult { IsCorrect = false, Feedback = "Krok nie znaleziony" };

                var result = new StepVerificationResult
                {
                    IsCorrect = false,
                    Feedback = "",
                    NextStep = stepIndex < lesson.Steps.Count - 1 ? stepIndex + 1 : null
                };

                // Weryfikacja odpowiedzi w zależności od typu kroku
                switch (step.Type.ToLower())
                {
                    case "quiz":
                        var quizData = JsonSerializer.Deserialize<QuizData>(step.AdditionalData);
                        result.IsCorrect = answer.ToString() == quizData.CorrectAnswer;
                        result.Feedback = result.IsCorrect
                            ? "Poprawna odpowiedź!"
                            : quizData.Explanation ?? "Niepoprawna odpowiedź";
                        break;

                    case "coding":
                    case "challenge":
                    case "interactive":
                    default:
                        result.IsCorrect = true;
                        result.Feedback = "Zadanie ukończone poprawnie!";
                        break;
                }

                // Oznacz krok jako ukończony, jeśli odpowiedź jest poprawna
                if (result.IsCorrect)
                {
                    await CompleteStepAsync(userId, lessonId, stepIndex, new StepCompletionData
                    {
                        IsCorrect = true,
                        TimeSpent = 0,
                        Attempts = 1
                    });
                }

                return result;
            }
            catch
            {
                return new StepVerificationResult
                {
                    IsCorrect = false,
                    Feedback = "Wystąpił błąd podczas weryfikacji"
                };
            }
        }

        public async Task<List<LessonRecommendationDto>> GetPersonalizedRecommendationsAsync(string userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserProgresses)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null) return new List<LessonRecommendationDto>();

                var completedLessonIds = user.UserProgresses
                    .Where(up => up.IsCompleted)
                    .Select(up => up.LessonId)
                    .ToHashSet(); // Używaj HashSet dla efektywniejszego wyszukiwania

                // Pobierz wszystkie moduły i ich lekcje
                var modules = await _moduleRepo.GetAllAsync();
                var recommendations = new List<LessonRecommendationDto>();

                // Pobierz moduły z lekcjami równolegle
                var moduleWithLessons = await Task.WhenAll(
                    modules.Select(m => _moduleRepo.GetModuleWithLessonsAsync(m.Id))
                );

                foreach (var module in moduleWithLessons)
                {
                    // Znajdź pierwszą nieukończoną lekcję w module
                    Lesson nextLesson = null;
                    // Fix for CS0103: The name 'moduleWithLessonsLessons' does not exist in the current context
                    // The variable 'moduleWithLessonsLessons' is not defined in the current context. Based on the surrounding code, it seems to be a typo or incorrect variable name.
                    // The correct variable name should likely be 'module.Lessons', as the context suggests iterating over lessons in a module.

                    foreach (var lesson in module.Lessons)
                    {
                        if (!completedLessonIds.Contains(lesson.Id))
                        {
                            nextLesson = lesson;
                            break;
                        }
              
                
                    }

                    if (nextLesson != null)
                    {
                        recommendations.Add(new LessonRecommendationDto
                        {
                            LessonId = nextLesson.ExternalId,
                            Title = nextLesson.Title,
                            Description = nextLesson.Description,
                            ModuleId = module.ExternalId,
                            ModuleTitle = module.Title,
                            RelevanceScore = CalculateRelevanceScore(nextLesson, user),
                            RecommendationType = "next_in_path"
                        });
                    }
                }

                // Zwróć top 5 najważniejszych
                recommendations.Sort((a, b) => b.RelevanceScore.CompareTo(a.RelevanceScore));
                return recommendations.Take(5).ToList();
            }
            catch
            {
                return new List<LessonRecommendationDto>();
            }
        }

        #region Metody pomocnicze

        private void ProcessStepAdditionalData(StepDto step)
        {
            if (string.IsNullOrEmpty(step.AdditionalData)) return;

            try
            {
                switch (step.Type.ToString().ToLower())
                {
                    case "quiz":
                        var quizData = JsonSerializer.Deserialize<QuizData>(step.AdditionalData);
                        step.Question = quizData.Question;
                        step.Options = quizData.Options;
                        step.CorrectAnswer = quizData.CorrectAnswer;
                        step.Explanation = quizData.Explanation;
                        break;

                    case "interactive":
                        var interactiveData = JsonSerializer.Deserialize<InteractiveData>(step.AdditionalData);
                        step.Items = interactiveData.Items;
                        break;

                    case "coding":
                    case "challenge":
                        var codingData = JsonSerializer.Deserialize<CodingData>(step.AdditionalData);
                        step.InitialCode = codingData.InitialCode;
                        step.TestCases = codingData.TestCases;
                        step.Hint = codingData.Hint;
                        step.Language = codingData.Language;
                        break;

                    case "video":
                        var videoData = JsonSerializer.Deserialize<VideoData>(step.AdditionalData);
                        step.VideoUrl = videoData.Url;
                        step.Duration = videoData.Duration;
                        step.RequireFullWatch = videoData.RequireFullWatch;
                        break;
                }
            }
            catch (JsonException)
            {
                // Logowanie błędu można dodać, ale nie rzucaj wyjątku
            }
        }

        private string CalculateTotalTime(IEnumerable<LessonDto> lessons)
        {
            if (lessons?.Any() != true)
                return "0 min";

            int totalMinutes = 0;
            foreach (var lesson in lessons)
            {
                if (string.IsNullOrEmpty(lesson.EstimatedTime)) continue;

                var time = lesson.EstimatedTime.Replace(" min", "");
                if (int.TryParse(time, out int minutes))
                    totalMinutes += minutes;
            }

            return $"{totalMinutes} min";
        }

        private string CalculateModuleDifficulty(IEnumerable<LessonDto> lessons)
        {
            if (lessons?.Any() != true)
                return "Beginner";

            var averageXP = lessons.Average(l => l.XpReward);
            if (averageXP < 20) return "Beginner";
            if (averageXP < 40) return "Intermediate";
            if (averageXP < 60) return "Advanced";
            return "Expert";
        }

        private int CalculateStepXP(Step step, StepCompletionData completionData)
        {
            switch (step.Type.ToLower())
            {
                case "quiz":
                    return completionData.IsCorrect.GetValueOrDefault() ? BASE_XP * 2 : BASE_XP;

                case "challenge":
                case "coding":
                    if (!completionData.TestsPassed.HasValue || !completionData.TotalTests.HasValue)
                        return BASE_XP;
                    return BASE_XP + (int)(15.0 * completionData.TestsPassed.Value / completionData.TotalTests.Value);

                case "interactive":
                    return completionData.IsCorrect.GetValueOrDefault() ? BASE_XP * 3 : BASE_XP;

                default:
                    return BASE_XP;
            }
        }

        private async Task<(bool IsValid, string Error)> ValidateStepCompletion(Step step, StepCompletionData completionData)
        {
            if (completionData == null)
                return (false, "Brak danych ukończenia");

            switch (step.Type.ToLower())
            {
                case "quiz":
                case "interactive":
                    if (!completionData.IsCorrect.HasValue)
                        return (false, "Brak informacji o poprawności");
                    break;

                case "challenge":
                case "coding":
                    if (!completionData.TestsPassed.HasValue || !completionData.TotalTests.HasValue)
                        return (false, "Brak informacji o testach");
                    if (completionData.TestsPassed.Value > completionData.TotalTests.Value)
                        return (false, "Nieprawidłowa liczba testów");
                    break;
            }

            return (true, null);
        }

        private async Task CheckAndAwardAchievements(string userId, Lesson lesson)
        {
            var user = await _context.Users
                .Include(u => u.UserProgresses)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return;

            var completedLessonsCount = user.UserProgresses.Count(p => p.IsCompleted);

            // Osiągnięcie za pierwszą lekcję
            if (completedLessonsCount == 1)
            {
                await _notifyService.SendNotificationAsync(
                    userId,
                    "Gratulacje! Ukończyłeś swoją pierwszą lekcję!",
                    "achievement"
                );
            }

            // Osiągnięcie za ukończenie modułu
            var moduleProgress = await GetModuleProgressAsync(userId, lesson.Module.ExternalId);
            if (moduleProgress.CompletionPercentage == 100)
            {
                await _notifyService.SendNotificationAsync(
                    userId,
                    $"Ukończyłeś moduł {lesson.Module.Title}!",
                    "achievement"
                );
            }
        }

        private async Task UpdateUserXpAsync(string userId, int xpToAdd)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            // Dodaj XP
            user.ExperiencePoints += xpToAdd;

            // Oblicz nowy poziom
            var oldLevel = user.Level;
            var level = 1;
            var requiredXp = BASE_LEVEL_XP;

            while (user.ExperiencePoints >= requiredXp)
            {
                level++;
                requiredXp = (int)(BASE_LEVEL_XP * Math.Pow(LEVEL_MULTIPLIER, level - 1));
            }

            user.Level = level;

            // Powiadomienie o nowym poziomie
            if (level > oldLevel)
            {
                await _notifyService.SendNotificationAsync(
                    userId,
                    $"Gratulacje! Osiągnąłeś poziom {level}!",
                    "level-up"
                );
            }

            await _context.SaveChangesAsync();
        }

        private int CalculateActivityXP(UserActionType actionType)
        {
            switch (actionType)
            {
                case UserActionType.LessonCompleted: return 50;
                case UserActionType.QuizCompleted: return 30;
                case UserActionType.StepCompleted: return 10;
                case UserActionType.ChallengePassed: return 40;
                case UserActionType.ModuleCompleted: return 100;
                default: return 0;
            }
        }

        private int CalculateXPToNextLevel(int currentXP, int currentLevel)
        {
            var nextLevelXP = (int)(BASE_LEVEL_XP * Math.Pow(LEVEL_MULTIPLIER, currentLevel));
            return nextLevelXP - currentXP;
        }

        private async Task<double> CalculateAverageCompletionRate(string userId)
        {
            var completionRates = await GetModuleCompletionRatesAsync(userId);
            return completionRates.Values.Any() ? completionRates.Values.Average() : 0;
        }

        private TimeSpan CalculateTotalLearningTime(ICollection<UserProgress> progresses)
        {
            var totalTime = TimeSpan.Zero;

            foreach (var progress in progresses)
            {
                if (progress.StartedAt.HasValue)
                {
                    var endTime = progress.CompletedAt ?? progress.LastUpdated ?? DateTime.UtcNow;
                    totalTime += endTime - progress.StartedAt.Value;
                }
            }

            return totalTime;
        }

        private async Task<Dictionary<string, int>> GetCompletedLessonsByType(string userId)
        {
            return await _context.UserProgress
                .Include(p => p.Lesson)
                .ThenInclude(l => l.Steps)
                .Where(p => p.UserId == userId && p.IsCompleted)
                .SelectMany(p => p.Lesson.Steps)
                .GroupBy(s => s.Type)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        private double CalculateRelevanceScore(Lesson lesson, ApplicationUser user)
        {
            double score = 1.0;

            // Czynniki podnoszące trafność rekomendacji
            if (lesson.XpReward <= user.Level * 10) score += 0.5;
            if (lesson.RequiredSkills == null || !lesson.RequiredSkills.Any()) score += 0.3;
            if (user.ExperiencePoints >= lesson.XpReward) score += 0.2;

            return Math.Min(score, 5.0);
        }

        #endregion
    }
}