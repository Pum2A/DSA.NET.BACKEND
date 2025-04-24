using System.Collections.Generic;

namespace DSA.Core.DTOs.Lessons
{
    public class StepDto
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Code { get; set; }
        public string Language { get; set; }
        public string ImageUrl { get; set; }
        public int Order { get; set; }
        public int LessonId { get; set; }

        // Pola dla quizów
        public string Question { get; set; }
        public List<QuizOptionDto> Options { get; set; }
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }

        // Pola dla interaktywnych zadań
        public string InitialCode { get; set; }
        public string ExpectedOutput { get; set; }
        public List<TestCaseDto> TestCases { get; set; }
        public string Solution { get; set; }
        public string Hint { get; set; }

        // Pola dla list
        public List<ListItemDto> Items { get; set; }
    }

    public class QuizOptionDto
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }

    public class TestCaseDto
    {
        public string Id { get; set; }
        public string Input { get; set; }
        public string ExpectedOutput { get; set; }
        public string Description { get; set; }
    }

    public class ListItemDto
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Description { get; set; }
    }
}