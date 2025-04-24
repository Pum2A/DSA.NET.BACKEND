using System;
namespace DSA.Core.DTOs.Lessons
{
    public class ModuleProgressDto
    {
        public int CompletedLessons { get; set; }
        public int InProgressLessons { get; set; }
        public int TotalLessons { get; set; }
    }
}