// DSA.// DSA.Core/Interfaces/ILessonService.cs
using DSA.Core.DTOs.Lessons.Learning;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DSA.Core.Interfaces
{
    public interface ILessonService
    {
        Task<IEnumerable<ModuleDto>> GetAllModulesAsync();
        Task<ModuleDto> GetModuleByIdAsync(string moduleId);
        Task<ModuleProgressDto> GetModuleProgressAsync(string userId, string moduleId);
        Task<LessonDto> GetLessonByIdAsync(string lessonId);
        Task<LessonProgressDto> GetLessonProgressAsync(string userId, string lessonId);
        Task<bool> CompleteStepAsync(string userId, string lessonId, int stepIndex, StepCompletionData completionData);
        Task<bool> CompleteLessonAsync(string userId, string lessonId);
        Task<UserLearningStatsDto> GetUserLearningStatsAsync(string userId);
        Task<Dictionary<string, double>> GetModuleCompletionRatesAsync(string userId);
        Task<List<RecentActivityDto>> GetRecentActivitiesAsync(string userId, int count = 10);
        Task<StepVerificationResult> VerifyStepAnswerAsync(string userId, string lessonId, int stepIndex, object answer);
        Task<List<LessonRecommendationDto>> GetPersonalizedRecommendationsAsync(string userId);
    }
}