
using DSA.Core.DTOs.Lessons;

namespace DSA.Core.Interfaces
{
    public interface ILessonService
    {
        Task<IEnumerable<ModuleDto>> GetAllModulesAsync();
        Task<ModuleDto> GetModuleByIdAsync(string moduleId);
        Task<LessonDto> GetLessonByIdAsync(string lessonId);
        Task<UserProgressDto> GetLessonProgressAsync(string userId, string lessonId);
        Task<ModuleProgressDto> GetModuleProgressAsync(string userId, string moduleId);
        Task<bool> CompleteStepAsync(string userId, string lessonId, int stepIndex);
        Task<bool> CompleteLessonAsync(string userId, string lessonId);
    }
}