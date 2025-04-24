using System;
using System.Threading.Tasks;
using DSA.Core.Entities;
using DSA.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace DSA.Infrastructure.Content
{
    public class ContentActivityLogger
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ContentActivityLogger> _logger;
        private readonly string _currentUser;

        public ContentActivityLogger(
            ApplicationDbContext dbContext,
            ILogger<ContentActivityLogger> logger,
            string currentUser = "system")
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task LogActivityAsync(
            string action,
            string source,
            string contentType,
            string contentId,
            string message,
            object additionalData = null)
        {
            try
            {
                var logEntry = new ContentActivityLog
                {
                    Action = action,
                    Source = source,
                    ContentType = contentType,
                    ContentId = contentId,
                    UserId = _currentUser,
                    UserName = _currentUser,
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = additionalData != null ?
                        System.Text.Json.JsonSerializer.Serialize(additionalData) : null
                };

                _dbContext.ContentActivityLogs.Add(logEntry);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    $"[Content] {action.ToUpper()} - {contentType} {contentId}: {message} by {_currentUser}");
            }
            catch (Exception ex)
            {
                // Nie pozwól, aby błąd logowania przerwał działanie aplikacji
                _logger.LogError(ex, $"Błąd podczas zapisywania aktywności treści: {ex.Message}");
            }
        }
    }
}