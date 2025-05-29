using System.Collections.Generic;

namespace DSA.Core.DTOs.Lessons
{
    public class LessonDto
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string EstimatedTime { get; set; }
        public int XpReward { get; set; }
        public int ModuleId { get; set; }
        public List<StepDto> Steps { get; set; }
    }
}