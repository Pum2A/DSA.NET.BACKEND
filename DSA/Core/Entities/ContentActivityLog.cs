using System;

namespace DSA.Core.Entities
{
    public class ContentActivityLog
    {
        public int Id { get; set; }
        public string Action { get; set; } // "load", "update", "error"
        public string Source { get; set; } // Źródło danych
        public string ContentType { get; set; } // "module", "lesson", "step"
        public string ContentId { get; set; } // ID treści
        public string UserId { get; set; } // ID użytkownika
        public string UserName { get; set; } // Nazwa użytkownika
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string AdditionalData { get; set; } // Dodatkowe dane w formacie JSON
    }
}