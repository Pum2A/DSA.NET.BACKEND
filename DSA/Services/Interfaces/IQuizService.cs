using System;
using System.Threading.Tasks;
using DSA.DTOs.Quizzes;

namespace DSA.Services
{
    public interface IQuizService
    {
        Task<ModuleQuizzesResponse> GetQuizzesForModuleAsync(Guid moduleId, Guid userId);
        Task<QuizDetailDto?> GetQuizDetailsAsync(Guid quizId, Guid userId);
        Task<QuizResultResponse> SubmitQuizAnswersAsync(Guid quizId, Guid userId, QuizSubmitRequest request);
        Task<UserQuizResultsResponse?> GetUserQuizResultsAsync(Guid quizId, Guid userId);
    }
}