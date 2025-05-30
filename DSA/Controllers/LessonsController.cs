using System;
using System.Threading.Tasks;
using DSA.DTOs.Lessons;
using DSA.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public LessonsController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        [HttpGet("modules")]
        public async Task<ActionResult<ModulesResponse>> GetModules()
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var modules = await _lessonService.GetModulesAsync(Guid.Parse(userId));
            return Ok(modules);
        }

        [HttpGet("modules/{moduleId}")]
        public async Task<ActionResult<ModuleDetailDto>> GetModuleDetails(Guid moduleId)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var module = await _lessonService.GetModuleDetailsAsync(moduleId, Guid.Parse(userId));

            if (module == null)
                return NotFound();

            return Ok(module);
        }

        [HttpGet("modules/{moduleId}/lessons")]
        public async Task<ActionResult<LessonsResponse>> GetModuleLessons(Guid moduleId)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var lessons = await _lessonService.GetLessonsInModuleAsync(moduleId, Guid.Parse(userId));
            return Ok(lessons);
        }

        [HttpGet("{lessonId}")]
        public async Task<ActionResult<LessonDetailDto>> GetLessonDetails(Guid lessonId)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var lesson = await _lessonService.GetLessonDetailsAsync(lessonId, Guid.Parse(userId));

            if (lesson == null)
                return NotFound();

            return Ok(lesson);
        }

        [HttpGet("{lessonId}/steps")]
        public async Task<ActionResult<LessonStepsResponse>> GetLessonSteps(Guid lessonId)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var steps = await _lessonService.GetLessonStepsAsync(lessonId, Guid.Parse(userId));

            if (steps == null)
                return NotFound();

            return Ok(steps);
        }

        [HttpPost("{lessonId}/steps/{stepId}/complete")]
        public async Task<ActionResult<StepCompleteResponse>> CompleteStep(Guid lessonId, Guid stepId)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _lessonService.CompleteStepAsync(lessonId, stepId, Guid.Parse(userId));

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("{lessonId}/complete")]
        public async Task<ActionResult<LessonCompleteResponse>> CompleteLesson(Guid lessonId)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _lessonService.CompleteLessonAsync(lessonId, Guid.Parse(userId));

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("{lessonId}/progress")]
        public async Task<ActionResult<LessonProgressDto>> GetLessonProgress(Guid lessonId)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var progress = await _lessonService.GetLessonProgressAsync(lessonId, Guid.Parse(userId));

            if (progress == null)
                return NotFound();

            return Ok(progress);
        }

        [HttpGet("progress")]
        public async Task<ActionResult<UserProgressResponse>> GetUserProgress()
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var progress = await _lessonService.GetUserProgressAsync(Guid.Parse(userId));
            return Ok(progress);
        }
    }
}