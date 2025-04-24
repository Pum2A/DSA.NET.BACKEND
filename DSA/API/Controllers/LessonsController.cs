using DSA.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using DSA.Core.Entities;
using DSA.Infrastructure.Data;
using DSA.Infrastructure;

namespace DSA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _lessonService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LessonsController> _logger;

        public LessonsController(
            ILessonService lessonService,
            ApplicationDbContext context,
            ILogger<LessonsController> logger)
        {
            _lessonService = lessonService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("modules")]
        public async Task<IActionResult> GetAllModules()
        {
            var modules = await _lessonService.GetAllModulesAsync();
            return Ok(modules);
        }

        [HttpGet("modules/{moduleId}")]
        public async Task<IActionResult> GetModule(string moduleId)
        {
            var module = await _lessonService.GetModuleByIdAsync(moduleId);
            if (module == null)
                return NotFound();

            return Ok(module);
        }

        [HttpGet("modules/{moduleId}/progress")]
        public async Task<IActionResult> GetModuleProgress(string moduleId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var progress = await _lessonService.GetModuleProgressAsync(userId, moduleId);
            if (progress == null)
                return NotFound();

            return Ok(progress);
        }

        [HttpGet("{lessonId}")]
        public async Task<IActionResult> GetLesson(string lessonId)
        {
            var lesson = await _lessonService.GetLessonByIdAsync(lessonId);
            if (lesson == null)
                return NotFound();

            return Ok(lesson);
        }

        [HttpGet("{lessonId}/progress")]
        public async Task<IActionResult> GetLessonProgress(string lessonId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var progress = await _lessonService.GetLessonProgressAsync(userId, lessonId);
            if (progress == null)
                return NotFound();

            return Ok(progress);
        }

        [HttpPost("{lessonId}/step/{stepIndex}/complete")]
        public async Task<IActionResult> CompleteStep(string lessonId, int stepIndex)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _lessonService.CompleteStepAsync(userId, lessonId, stepIndex);
            if (!result)
                return BadRequest("Failed to complete step");

            return Ok();
        }

        [HttpPost("{lessonId}/complete")]
        public async Task<IActionResult> CompleteLesson(string lessonId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                // Sprawdź, czy lekcja istnieje
                var lesson = await _context.Lessons
                    .FirstOrDefaultAsync(l => l.ExternalId == lessonId);

                if (lesson == null)
                {
                    _logger.LogWarning($"[LessonsController] Lekcja o id {lessonId} nie istnieje w bazie!");
                    return NotFound($"Nie znaleziono lekcji o ID {lessonId}");
                }

                // Sprawdź, czy lekcja była już ukończona
                var existingProgress = await _context.UserProgresses
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lesson.Id);

                bool wasAlreadyCompleted = existingProgress != null && existingProgress.IsCompleted;

                // Wywołaj serwis by ukończyć lekcję
                var result = await _lessonService.CompleteLessonAsync(userId, lessonId);

                if (!result)
                {
                    _logger.LogWarning($"[LessonsController] Nie udało się ukończyć lekcji {lessonId} dla użytkownika {userId}");
                    return BadRequest("Nie udało się ukończyć lekcji");
                }

                // Przygotuj odpowiednią odpowiedź
                if (wasAlreadyCompleted)
                {
                    _logger.LogInformation($"[LessonsController] Lekcja {lessonId} była już wcześniej ukończona przez użytkownika {userId}");
                    return Ok(new
                    {
                        success = true,
                        xpAwarded = 0,
                        message = "Lekcja została już wcześniej ukończona. Nie przyznano dodatkowego XP."
                    });
                }
                else
                {
                    _logger.LogInformation($"[LessonsController] Użytkownik {userId} ukończył lekcję {lessonId} i otrzymał {lesson.XpReward} XP");
                    return Ok(new
                    {
                        success = true,
                        xpAwarded = lesson.XpReward,
                        message = $"Gratulacje! Ukończyłeś lekcję i zdobyłeś {lesson.XpReward} XP!"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[LessonsController] Błąd podczas ukończenia lekcji {lessonId} dla użytkownika {userId}");
                return StatusCode(500, $"Wystąpił nieoczekiwany błąd: {ex.Message}");
            }
        }

        [HttpGet("{lessonId}/steps")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLessonSteps(string lessonId)
        {
            try
            {
                _logger.LogInformation($"[LessonsController] Pobieranie kroków dla lekcji: {lessonId}");

                // Pobierz lekcję po externalId
                var lesson = await _context.Lessons
                    .FirstOrDefaultAsync(l => l.ExternalId == lessonId);

                if (lesson == null)
                {
                    _logger.LogWarning($"[LessonsController] Lekcja o id {lessonId} nie istnieje w bazie!");
                    return NotFound($"Nie znaleziono lekcji o ID {lessonId}");
                }

                _logger.LogInformation($"[LessonsController] Znaleziono lekcję: Id={lesson.Id}, ExternalId={lesson.ExternalId}, Title={lesson.Title}");

                // Pobierz kroki dla lekcji
                var steps = await _context.Steps
                    .Where(s => s.LessonId == lesson.Id)
                    .OrderBy(s => s.Order)
                    .ToListAsync();

                _logger.LogInformation($"[LessonsController] Znaleziono {steps.Count} kroków dla lekcji {lessonId}");

                if (!steps.Any())
                {
                    _logger.LogWarning($"[LessonsController] Brak kroków dla lekcji {lessonId}!");

                    // UWAGA: Zamiast zwracać 404, zwróćmy pustą listę
                    return Ok(new List<object>());
                }

                // Reszta kodu przetwarzającego kroki bez zmian...
                var result = steps.Select(step =>
                {
                    // Tworzymy słownik, który będzie zawierał wszystkie właściwości
                    var properties = new Dictionary<string, object>
                    {
                        ["id"] = step.Id,
                        ["type"] = step.Type,
                        ["title"] = step.Title,
                        ["content"] = step.Content,
                        ["code"] = step.Code,
                        ["language"] = step.Language,
                        ["imageUrl"] = step.ImageUrl,
                        ["order"] = step.Order,
                        ["lessonId"] = step.LessonId
                    };

                    // Jeśli to quiz lub interactive, spróbuj sparsować additionalData
                    if ((step.Type == "quiz" || step.Type == "interactive") && !string.IsNullOrEmpty(step.AdditionalData))
                    {
                        try
                        {
                            // Konwertuj additionalData na słownik
                            var additionalData = JsonSerializer.Deserialize<Dictionary<string, object>>(step.AdditionalData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            // Przenieś pola additionalData do głównego obiektu
                            foreach (var key in additionalData.Keys)
                            {
                                properties[key] = additionalData[key];
                            }

                            // Specjalne przetwarzanie dla quiz
                            if (step.Type == "quiz" && additionalData.ContainsKey("options"))
                            {
                                // Pobierz opcje i przetwórz je poprawnie
                                var optionsElement = (JsonElement)additionalData["options"];
                                var options = JsonSerializer.Deserialize<List<object>>(optionsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                properties["options"] = options;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[LessonsController] Błąd parsowania additionalData dla kroku {StepId}", step.Id);
                            properties["parseError"] = "Błąd przetwarzania danych dodatkowych";
                        }
                    }

                    // Zachowaj też oryginalne additionalData
                    properties["additionalData"] = step.AdditionalData;

                    return properties;
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LessonsController] Błąd podczas pobierania kroków dla lekcji {LessonId}", lessonId);
                return StatusCode(500, $"Wystąpił błąd podczas przetwarzania żądania: {ex.Message}");
            }
        }
    }
}