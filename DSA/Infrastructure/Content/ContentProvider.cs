using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DSA.Core.Entities;

namespace DSA.Infrastructure.Content
{
    public class ContentProvider
    {
        private readonly ILogger<ContentProvider> _logger;
        private readonly List<IContentSource> _contentSources;

        public ContentProvider(ILogger<ContentProvider> logger)
        {
            _logger = logger;
            _contentSources = new List<IContentSource>();
        }

        // Rejestruj źródła treści
        public void RegisterContentSource(IContentSource source)
        {
            _contentSources.Add(source);
            _logger.LogInformation($"Zarejestrowano źródło treści: {source.GetType().Name}");
        }

        // Ładuj wszystkie dane
        public async Task LoadAllContentAsync(ContentContext context)
        {
            _logger.LogInformation("ContentProvider: Starting content loading process...");

            int sourceCount = _contentSources.Count;
            _logger.LogInformation($"ContentProvider: Found {sourceCount} registered sources");

            foreach (var source in _contentSources)
            {
                _logger.LogInformation($"ContentProvider: Loading content from source: {source.GetType().Name}");
                try
                {
                    await source.LoadContentAsync(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"ContentProvider: Error loading content from source: {source.GetType().Name}");
                    context.ValidationReport.AddIssue("ContentProvider",
                        $"Error loading content from {source.GetType().Name}: {ex.Message}",
                        ContentIssueSeverity.Error);
                }
            }
        }
    }

    // Interfejs dla źródeł treści
    public interface IContentSource
    {
        Task LoadContentAsync(ContentContext context);
    }

    // Kontekst ładowania treści
    public class ContentContext
    {
        public ApplicationDbContext DbContext { get; }
        public ContentValidationReport ValidationReport { get; }
        public bool StrictMode { get; set; } = false;

        public ContentContext(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
            ValidationReport = new ContentValidationReport();
        }
    }

    // Raport walidacji
    public class ContentValidationReport
    {
        public List<ContentValidationIssue> Issues { get; } = new List<ContentValidationIssue>();

        public void AddIssue(string source, string message, ContentIssueSeverity severity = ContentIssueSeverity.Warning)
        {
            Issues.Add(new ContentValidationIssue
            {
                Source = source,
                Message = message,
                Severity = severity,
                Timestamp = DateTime.UtcNow
            });
        }

        public bool HasErrors => Issues.Exists(i => i.Severity == ContentIssueSeverity.Error);
    }

    public class ContentValidationIssue
    {
        public string Source { get; set; }
        public string Message { get; set; }
        public ContentIssueSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum ContentIssueSeverity
    {
        Info,
        Warning,
        Error
    }
}