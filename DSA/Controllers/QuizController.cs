using System;
using System.Threading.Tasks;
using DSA.DTOs.Quizzes;
using DSA.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public QuizController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [HttpGet("modules/{moduleId}")]
        public async Task<ActionResult<ModuleQuizzesResponse>> GetModuleQuizzes(Guid moduleId)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var quizzes = await _quizService.GetQuizzesForModuleAsync(moduleId, Guid.Parse(userId));
            return Ok(quizzes);
        }

        [HttpGet("{quizId}")]
        public async Task<ActionResult<QuizDetailDto>> GetQuiz(Guid quizId)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var quiz = await _quizService.GetQuizDetailsAsync(quizId, Guid.Parse(userId));

            if (quiz == null)
                return NotFound();

            return Ok(quiz);
        }

        [HttpPost("{quizId}/submit")]
        public async Task<ActionResult<QuizResultResponse>> SubmitQuizAnswers(Guid quizId, QuizSubmitRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _quizService.SubmitQuizAnswersAsync(quizId, Guid.Parse(userId), request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("{quizId}/results")]
        public async Task<ActionResult<UserQuizResultsResponse>> GetQuizResults(Guid quizId)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var results = await _quizService.GetUserQuizResultsAsync(quizId, Guid.Parse(userId));

            if (results == null)
                return NotFound();

            return Ok(results);
        }
    }
}