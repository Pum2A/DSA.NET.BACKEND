using System.Threading.Tasks;
using DSA.Infrastructure.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DSA.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DSA.API.Controllers
{
    [ApiController]
    [Route("api/admin/content")]
    [Authorize(Roles = "Admin")]  // Tylko administratorzy
    public class AdminContentController : ControllerBase
    {
        private readonly ContentProvider _contentProvider;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AdminContentController> _logger;

        public AdminContentController(
            ContentProvider contentProvider,
            ApplicationDbContext dbContext,
            ILogger<AdminContentController> logger)
        {
            _contentProvider = contentProvider;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost("reload")]
        public async Task<IActionResult> ReloadContent()
        {
            _logger.LogInformation("Rozpoczynam ponowne ładowanie treści na żądanie administratora");

            var context = new ContentContext(_dbContext);
            await _contentProvider.LoadAllContentAsync(context);

            // Przygotuj raport
            var report = new
            {
                Success = !context.ValidationReport.HasErrors,
                IssuesCount = context.ValidationReport.Issues.Count,
                ErrorsCount = context.ValidationReport.Issues.Count(i => i.Severity == ContentIssueSeverity.Error),
                WarningsCount = context.ValidationReport.Issues.Count(i => i.Severity == ContentIssueSeverity.Warning),
                Issues = context.ValidationReport.Issues
            };

            return Ok(report);
        }

        [HttpGet("validation-report")]
        public async Task<IActionResult> GetValidationReport()
        {
            _logger.LogInformation("Pobieranie raportu walidacji treści");

            var context = new ContentContext(_dbContext) { StrictMode = false };

            // Uruchom walidację bez zapisywania
            var tempLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ContentProvider>();
            var tempProvider = new ContentProvider(tempLogger);
            // Tu możesz dodać tylko źródła walidujące, bez zapisywania

            await tempProvider.LoadAllContentAsync(context);

            return Ok(context.ValidationReport);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetContentStats()
        {
            var modulesCount = await _dbContext.Modules.CountAsync();
            var lessonsCount = await _dbContext.Lessons.CountAsync();
            var stepsCount = await _dbContext.Steps.CountAsync();

            var stats = new
            {
                ModulesCount = modulesCount,
                LessonsCount = lessonsCount,
                StepsCount = stepsCount,
                LessonsWithoutSteps = await _dbContext.Lessons
                    .Where(l => !_dbContext.Steps.Any(s => s.LessonId == l.Id))
                    .Select(l => new { l.Id, l.ExternalId, l.Title })
                    .ToListAsync()
            };

            return Ok(stats);
        }
    }
}