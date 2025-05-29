using System.Text.Json;

namespace DSA.Core.Entities
{
    public class Step
    {
        public int Id { get; set; }
        public string Type { get; set; } // text, image, code, quiz, interactive, challenge, list
        public string Title { get; set; }
        public string Content { get; set; }

        // Zmień na nullable
        public string? Code { get; set; } = null;
        public string? Language { get; set; } = null;
        public string? ImageUrl { get; set; } = null;


        public int Order { get; set; }

        public int LessonId { get; set; }
        public Lesson? Lesson { get; set; }

        // Już zmieniliśmy to pole wcześniej
        public string? AdditionalData { get; set; } = null;

        // Metody pomocnicze do pracy z dodatkowymi danymi
        public T? GetAdditionalData<T>() where T : class
        {
            if (string.IsNullOrEmpty(AdditionalData))
                return null;

            return JsonSerializer.Deserialize<T>(AdditionalData);
        }

        public void SetAdditionalData<T>(T data) where T : class
        {
            AdditionalData = data != null ? JsonSerializer.Serialize(data) : null;
        }
    }
}