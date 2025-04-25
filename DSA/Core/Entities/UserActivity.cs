namespace DSA.Core.Entities
{
    public enum UserActionType
    {
        LessonCompleted,
        QuizCompleted,
        Login,
        // Dodaj kolejne typy w razie potrzeby
    }

    public class UserActivity
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public UserActionType ActionType { get; set; }
        public DateTime ActionTime { get; set; }
        public string? ReferenceId { get; set; } // np. Id lekcji lub quizu (opcjonalnie)
        public string? AdditionalInfo { get; set; }
    }
}
