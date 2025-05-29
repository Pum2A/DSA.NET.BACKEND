using DSA.Core.Entities;
using DSA.Core.Helpers;
using DSA.Core.Interfaces;
using DSA.Infrastructure;
using DSA.Infrastructure.Data;
using DSA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DSA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        private readonly RankingService _rankingService;


        public UserController(
            ApplicationDbContext context,
            IUserService userService,
            ILogger<UserController> logger,
            RankingService rankingService)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
            _rankingService = rankingService;
            
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetUserStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                _logger.LogInformation($"Fetching stats for user {userId}");

                var user = await _userService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning($"User {userId} not found");
                    return NotFound(new { succeeded = false, errors = new[] { "Użytkownik nie znaleziony" } });
                }

                var completedLessons = await _context.UserProgress
                    .Where(p => p.UserId == userId && p.IsCompleted)
                    .CountAsync();

                var totalLessons = await _context.Lessons.CountAsync();

                // Nowy system poziomów!
                var level = LevelingHelper.GetLevelForXp(user.ExperiencePoints);
                var currentLevelMinXp = LevelingHelper.GetXpForCurrentLevel(level);
                var requiredXpForNextLevel = LevelingHelper.GetXpForNextLevel(level);
                var xpToNextLevel = requiredXpForNextLevel - user.ExperiencePoints;

                var stats = new
                {
                    userId = userId,
                    userName = user.UserName,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    level = level,
                    totalXp = user.ExperiencePoints,
                    currentLevelMinXp = currentLevelMinXp,
                    requiredXpForNextLevel = requiredXpForNextLevel,
                    xpToNextLevel = xpToNextLevel,
                    completedLessonsCount = completedLessons,
                    totalLessonsCount = totalLessons,
                    joinedAt = user.JoinedAt,
                    // więcej statystyk według potrzeb
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching stats for user {userId}");
                return StatusCode(500, new { succeeded = false, errors = new[] { $"Błąd podczas pobierania statystyk: {ex.Message}" } });
            }
        }

        [HttpGet("progress")]
        public async Task<IActionResult> GetUserProgress()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                _logger.LogInformation($"Fetching progress for user {userId}");

                // Pobierz postęp użytkownika z wczesnym ładowaniem lekcji
                var progressRecords = await _context.UserProgress
                    .Where(p => p.UserId == userId)
                    .ToListAsync();

                // Pobierz wszystkie moduły z lekcjami w jednym zapytaniu
                var modules = await _context.Modules
                    .Include(m => m.Lessons)
                    .ToListAsync();

                // Indeksuj progres użytkownika według ID lekcji dla szybkiego wyszukiwania
                var progressByLessonId = progressRecords
                    .ToDictionary(p => p.LessonId, p => p);

                // Przygotuj dane modułów z obliczonymi statystykami
                var modulesWithProgress = modules.Select(m =>
                {
                    // Policz lekcje dla tego modułu
                    var moduleId = m.Id;
                    var moduleLessons = m.Lessons.ToList();
                    var lessonsCount = moduleLessons.Count;

                    // Policz ukończone lekcje
                    var completedLessons = 0;
                    foreach (var lesson in moduleLessons)
                    {
                        if (progressByLessonId.TryGetValue(lesson.Id, out var lessonProgress) &&
                            lessonProgress.IsCompleted)
                        {
                            completedLessons++;
                        }
                    }

                    // Oblicz procent ukończenia
                    var progressPercent = lessonsCount > 0
                        ? (int)Math.Round((double)completedLessons / lessonsCount * 100)
                        : 0;

                    return new
                    {
                        moduleId = moduleId.ToString(),
                        externalId = m.ExternalId,
                        title = m.Title,
                        description = m.Description,
                        lessonsCount = lessonsCount,
                        completedLessons = completedLessons,
                        progress = progressPercent
                    };
                }).ToList();

                // Przygotuj informacje o postępie lekcji
                var lessonProgressInfo = progressRecords.Select(p => new
                {
                    lessonId = p.LessonId.ToString(), // Konwersja do stringa dla API
                    completed = p.IsCompleted,
                    currentStepIndex = p.CurrentStepIndex,
                    lastUpdated = p.CompletedAt ?? p.StartedAt ?? DateTime.UtcNow
                }).ToList();

                // Zwróć kompletne dane
                return Ok(new
                {
                    userId = userId,
                    modules = modulesWithProgress,
                    lessonProgress = lessonProgressInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching progress for user {userId}");
                return StatusCode(500, new { succeeded = false, errors = new[] { $"Błąd podczas pobierania postępu: {ex.Message}" } });
            }
        }

        [HttpPost("add-xp")]
        public async Task<IActionResult> AddExperience([FromBody] AddExperienceRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                if (request.Amount <= 0)
                {
                    return BadRequest(new { succeeded = false, errors = new[] { "Wartość XP musi być większa od 0" } });
                }

                var success = await _userService.AddExperienceAsync(userId, request.Amount);
                if (!success)
                {
                    return NotFound(new { succeeded = false, errors = new[] { "Użytkownik nie znaleziony" } });
                }

                // Pobierz zaktualizowane dane użytkownika
                var user = await _userService.GetUserByIdAsync(userId);

                return Ok(new
                {
                    succeeded = true,
                    userId = userId,
                    currentXp = user.ExperiencePoints,
                    level = user.Level,
                    xpAdded = request.Amount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding XP for user {userId}");
                return StatusCode(500, new { succeeded = false, errors = new[] { $"Błąd podczas dodawania XP: {ex.Message}" } });
            }
        }


        [HttpGet("ranking/level")]
        public async Task<IActionResult> GetRankingByLevel(int page = 1, int limit = 10)
        {
            var ranking = await _rankingService.GetRankingByLevelAsync(page, limit);
            return Ok(ranking);
        }

        [HttpGet("ranking/streak")]
        public async Task<IActionResult> GetRankingByStreak(int page = 1, int limit = 10)
        {
            var ranking = await _rankingService.GetRankingByStreakAsync(page, limit);
            return Ok(ranking);
        }

        [HttpGet("ranking/joined-time")]
        public async Task<IActionResult> GetRankingByJoinedTime(int page = 1, int limit = 10)
        {
            var ranking = await _rankingService.GetRankingByJoinedTimeAsync(page, limit);
            return Ok(ranking);
        }

        // NOWA METODA: ukończenie lekcji!
        [HttpPost("complete-lesson")]
        public async Task<IActionResult> CompleteLesson([FromBody] CompleteLessonRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            if (request.LessonId <= 0) // Fix: Changed from Guid.Empty to check if LessonId is less than or equal to 0
            {
                return BadRequest(new { succeeded = false, errors = new[] { "Nieprawidłowe ID lekcji" } });
            }

            try
            {
                var progress = await _context.UserProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == request.LessonId);

                if (progress == null)
                {
                    // Jeśli nie ma progresu, utwórz nowy
                    progress = new UserProgress
                    {
                        UserId = userId,
                        LessonId = request.LessonId,
                        IsCompleted = true,
                        CompletedAt = DateTime.UtcNow,
                        StartedAt = DateTime.UtcNow,
                        CurrentStepIndex = 0
                    };
                    _context.UserProgress.Add(progress);
                }
                else if (!progress.IsCompleted)
                {
                    progress.IsCompleted = true;
                    progress.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    // Już ukończona lekcja
                    return Ok(new { succeeded = true, alreadyCompleted = true });
                }

                await _context.SaveChangesAsync();

                // KLUCZOWE: sprawdź osiągnięcia!
                await _userService.CheckAndNotifyLessonAchievementsAsync(userId);

                return Ok(new { succeeded = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas ukończenia lekcji dla usera {userId}: {ex.Message}");
                return StatusCode(500, new { succeeded = false, errors = new[] { $"Błąd podczas ukończenia lekcji: {ex.Message}" } });
            }


        }


    }


    public class AddExperienceRequest
    {
        public int Amount { get; set; }
    }

    public class CompleteLessonRequest
    {
        public int LessonId { get; set; }
    }
}
// Compare this snippet from DSA/Infrastructure/Services/UserService.cs:
