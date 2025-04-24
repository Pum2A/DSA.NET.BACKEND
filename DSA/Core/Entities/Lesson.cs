using System.Collections.Generic;

namespace DSA.Core.Entities
{
    public class Lesson
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string EstimatedTime { get; set; }
        public int XpReward { get; set; }

        public int ModuleId { get; set; }
        public Module Module { get; set; }

        public ICollection<Step> Steps { get; set; }
        public ICollection<UserProgress> UserProgresses { get; set; }
    }
}