using System.Collections.Generic;

namespace DSA.Core.Entities
{
    public class Module
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public string Icon { get; set; }
        public string IconColor { get; set; }

        public ICollection<Lesson> Lessons { get; set; }
    }
}