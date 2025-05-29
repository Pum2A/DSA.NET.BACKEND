using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using DSA.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DSA.Infrastructure.Content.Sources
{
    /// <summary>
    /// Adapter specjalnie do naprawy problemu "stack-queue"
    /// </summary>
    public class StackQueueAdapter : IContentSource
    {
        private readonly ILogger<StackQueueAdapter> _logger;

        public StackQueueAdapter(ILogger<StackQueueAdapter> logger)
        {
            _logger = logger;
        }

        public async Task LoadContentAsync(ContentContext context)
        {
            _logger.LogInformation("Uruchamiam adapter dla stack-queue");

            // Znajdź lekcję stack-queue po ExternalId
            var lesson = await context.DbContext.Lessons
                .FirstOrDefaultAsync(l => l.ExternalId == "stack-queue");

            if (lesson == null)
            {
                _logger.LogWarning("Lekcja 'stack-queue' nie istnieje w bazie danych!");
                context.ValidationReport.AddIssue("StackQueueAdapter",
                    "Lekcja 'stack-queue' nie istnieje w bazie danych",
                    ContentIssueSeverity.Warning);
                return;
            }

            // Sprawdź czy już istnieją kroki
            var stepsCount = await context.DbContext.Steps
                .Where(s => s.LessonId == lesson.Id)
                .CountAsync();

            if (stepsCount > 0)
            {
                _logger.LogInformation($"Lekcja 'stack-queue' już ma {stepsCount} kroków.");
                return;
            }

            _logger.LogWarning($"Lekcja 'stack-queue' (ID: {lesson.Id}) nie ma kroków! Dodaję awaryjne kroki.");

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
                    Content = "Stos to struktura danych działająca na zasadzie LIFO (Last In, First Out) - ostatni element dodany jest pierwszym, który zostanie pobrany.",
                    Order = 2
                },
                new Step
                {
                    LessonId = lesson.Id,
                    Type = "code",
                    Title = "Implementacja stosu",
                    Content = "Poniżej znajdziesz przykładową implementację stosu w JavaScript:",
                    Code = "class Stack {\n  constructor() {\n    this.items = [];\n  }\n\n  push(element) {\n    this.items.push(element);\n  }\n\n  pop() {\n    if (this.isEmpty()) return \"Underflow\";\n    return this.items.pop();\n  }\n\n  peek() {\n    return this.items[this.items.length - 1];\n  }\n\n  isEmpty() {\n    return this.items.length === 0;\n  }\n}",
                    Language = "javascript",
                    Order = 3
                },
                new Step
                {
                    LessonId = lesson.Id,
                    Type = "text",
                    Title = "Kolejka (Queue)",
                    Content = "Kolejka to struktura danych działająca na zasadzie FIFO (First In, First Out) - pierwszy element dodany jest pierwszym, który zostanie pobrany.",
                    Order = 4
                },
                new Step
                {
                    LessonId = lesson.Id,
                    Type = "code",
                    Title = "Implementacja kolejki",
                    Content = "Poniżej znajdziesz przykładową implementację kolejki w JavaScript:",
                    Code = "class Queue {\n  constructor() {\n    this.items = [];\n  }\n\n  enqueue(element) {\n    this.items.push(element);\n  }\n\n  dequeue() {\n    if (this.isEmpty()) return \"Underflow\";\n    return this.items.shift();\n  }\n\n  front() {\n    if (this.isEmpty()) return \"Queue is empty\";\n    return this.items[0];\n  }\n\n  isEmpty() {\n    return this.items.length === 0;\n  }\n}",
                    Language = "javascript",
                    Order = 5
                },
                new Step
                {
                    LessonId = lesson.Id,
                    Type = "quiz",
                    Title = "Quiz: Stosy i kolejki",
                    Content = "Sprawdź swoją wiedzę o stosach i kolejkach:",
                    AdditionalData = "{\"question\":\"Która struktura danych działa na zasadzie LIFO?\",\"options\":[{\"id\":\"1\",\"text\":\"Stos\",\"correct\":true},{\"id\":\"2\",\"text\":\"Kolejka\",\"correct\":false},{\"id\":\"3\",\"text\":\"Lista\",\"correct\":false},{\"id\":\"4\",\"text\":\"Drzewo\",\"correct\":false}],\"correctAnswer\":\"1\",\"explanation\":\"Stos (Stack) działa na zasadzie LIFO (Last In, First Out), co oznacza, że ostatni element dodany jest pierwszym, który zostanie usunięty.\"}",
                    Order = 6
                }
            };

            context.DbContext.Steps.AddRange(steps);
            await context.DbContext.SaveChangesAsync();

            _logger.LogInformation($"Naprawiono lekcję stack-queue, dodając {steps.Count} kroków.");
        }
    }
}