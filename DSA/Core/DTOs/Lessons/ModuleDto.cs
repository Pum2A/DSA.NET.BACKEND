using System.Collections.Generic;

namespace DSA.Core.DTOs.Lessons
{
    public class ModuleDto
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public string Icon { get; set; }
        public string IconColor { get; set; }
        public List<LessonDto> Lessons { get; set; }
    }
}