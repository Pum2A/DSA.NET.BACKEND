using System;

namespace DSA.Core.DTOs.Lessons
{
    public class UserProgressDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int LessonId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int CurrentStepIndex { get; set; }
        public int XpEarned { get; set; }
    }
}