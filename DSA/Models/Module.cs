using System;
using System.Collections.Generic;

namespace DSA.Models
{
    public class Module
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsActive { get; set; }

        // Navigation properties
        public List<Lesson> Lessons { get; set; } = new();
        public List<Quiz> Quizzes { get; set; } = new();
    }
}