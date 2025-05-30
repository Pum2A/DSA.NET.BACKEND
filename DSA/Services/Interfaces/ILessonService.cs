using System;
using System.Threading.Tasks;
using DSA.DTOs.Lessons;

namespace DSA.Services
{
    public interface ILessonService
    {
        Task<ModulesResponse> GetModulesAsync(Guid userId);
        Task<ModuleDetailDto?> GetModuleDetailsAsync(Guid moduleId, Guid userId);
        Task<LessonsResponse> GetLessonsInModuleAsync(Guid moduleId, Guid userId);
        Task<LessonDetailDto?> GetLessonDetailsAsync(Guid lessonId, Guid userId);
        Task<LessonStepsResponse?> GetLessonStepsAsync(Guid lessonId, Guid userId);
        Task<StepCompleteResponse> CompleteStepAsync(Guid lessonId, Guid stepId, Guid userId);
        Task<LessonCompleteResponse> CompleteLessonAsync(Guid lessonId, Guid userId);
        Task<LessonProgressDto?> GetLessonProgressAsync(Guid lessonId, Guid userId);
        Task<UserProgressResponse> GetUserProgressAsync(Guid userId);
    }
}