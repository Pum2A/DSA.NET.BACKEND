using System;

namespace DSA.Core.Entities
{
    public class UserProgress
    {
        public int Id { get; set; }  // Klucz główny
        public string UserId { get; set; }
        public int LessonId { get; set; }
        public bool IsCompleted { get; set; }  // Zmieniono z Completed na IsCompleted
        public int CurrentStepIndex { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastUpdated { get; set; }

        public int XpEarned { get; set; }
        
        
        // Właściwości nawigacyjne
        public virtual ApplicationUser User { get; set; }
        public virtual Lesson Lesson { get; set; }
    }
}