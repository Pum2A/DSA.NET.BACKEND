using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DSA.Core.Entities.Learning;

namespace DSA.Infrastructure.Content.Sources
{
    public class StackQueueAdapter : IContentSource
    {
        private readonly ILogger<StackQueueAdapter> _logger;

        public StackQueueAdapter(ILogger<StackQueueAdapter> logger) => _logger = logger;

        public async Task LoadContentAsync(ContentContext context)
        {
            // Znajdź lekcję stack-queue
            var lesson = await context.DbContext.Lessons
                .FirstOrDefaultAsync(l => l.ExternalId == "stack-queue");

            if (lesson == null || await context.DbContext.Steps.AnyAsync(s => s.LessonId == lesson.Id))
                return;

            _logger.LogWarning($"Lekcja 'stack-queue' bez kroków. Dodaję awaryjne kroki.");

            // Dodaj kroki dla lekcji
            var steps = new List<Step>
            {
                new Step
                {
                    LessonId = lesson.Id,
                    Type = "text",
                    Title = "Wprowadzenie do stosów i kolejek",
                    Content = "Stosy i kolejki to podstawowe struktury danych, które działają na zasadzie ograniczonego dostępu do elementów.",
                    Order = 1
                },
                new Step
                {
                    LessonId = lesson.Id,
                    Type = "text",
                    Title = "Stos (Stack)",
                    Content = "Stos to struktura danych działająca na zasadzie LIFO (Last In, First Out).",
                    Order = 2
                },
                // Więcej kroków można dodać w razie potrzeby
            };

            context.DbContext.Steps.AddRange(steps);
            await context.DbContext.SaveChangesAsync();
        }
    }
}