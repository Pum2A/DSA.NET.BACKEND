using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DSA.Core.Entities;
using DSA.Infrastructure.Data;

namespace DSA.Infrastructure.Content
{
    public class ContentProvider
    {
        private readonly ILogger<ContentProvider> _logger;
        private readonly List<IContentSource> _contentSources = new();

        public ContentProvider(ILogger<ContentProvider> logger) => _logger = logger;

        public void RegisterContentSource(IContentSource source)
        {
            _contentSources.Add(source);
            _logger.LogInformation($"Zarejestrowano źródło: {source.GetType().Name}");
        }

        public async Task LoadAllContentAsync(ContentContext context)
        {
            _logger.LogInformation($"Ładowanie treści z {_contentSources.Count} źródeł");

            foreach (var source in _contentSources)
            {
                try
                {
                    await source.LoadContentAsync(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Błąd źródła: {source.GetType().Name}");
                    context.ValidationReport.AddIssue("ContentProvider", ex.Message, ContentIssueSeverity.Error);
                }
            }
        }
    }

    public interface IContentSource
    {
        Task LoadContentAsync(ContentContext context);
    }

    public class ContentContext
    {
        public ApplicationDbContext DbContext { get; }
        public ContentValidationReport ValidationReport { get; } = new();
        public bool StrictMode { get; set; } = false;

        public ContentContext(ApplicationDbContext dbContext) => DbContext = dbContext;
    }

    public class ContentValidationReport
    {
        public List<ContentValidationIssue> Issues { get; } = new();

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