using System;
using System.Collections.Generic;
using System.Linq;
using DSA.Models;

namespace DSA.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext db)
        {
            if (db.Modules.Any())
                return; // Prevent duplicate seeding

            // --- MODULE 1: Wprowadzenie do struktur danych ---
            var moduleIntro = new Module
            {
                Id = Guid.NewGuid(),
                Title = "Wprowadzenie do struktur danych",
                Description = "Dowiedz się, czym są struktury danych, dlaczego są ważne i jakie są ich najważniejsze rodzaje.",
                IconUrl = "/modules/intro.png",
                Order = 1,
                IsActive = true
            };

            var lessonIntro1 = new Lesson
            {
                Id = Guid.NewGuid(),
                Module = moduleIntro,
                Title = "Czym są struktury danych?",
                Description = "Podstawowe pojęcia, przykłady i zastosowania struktur danych.",
                XpReward = 40,
                Order = 1,
                IsActive = true,
                Steps = new List<LessonStep>
                {
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Definicja struktury danych",
                        Content = "Struktury danych to sposoby organizacji i przechowywania danych, aby można było efektywnie z nich korzystać. W programowaniu, dobór odpowiedniej struktury danych wpływa na wydajność i czytelność kodu.",
                        Order = 1
                    },
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Przykłady struktur danych",
                        Content = "Najpopularniejsze struktury danych to: tablica, lista, stos, kolejka, drzewo, graf. Każda z nich ma swoje zalety i wady oraz zastosowania.",
                        Order = 2
                    },
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Zastosowania struktur danych",
                        Content = "Przykładowo: stosy używane są w przeglądarkach (cofanie do poprzedniej strony), kolejki w systemach kolejkowania zadań, drzewa w bazach danych i systemach plików.",
                        Order = 3
                    }
                }
            };

            var lessonIntro2 = new Lesson
            {
                Id = Guid.NewGuid(),
                Module = moduleIntro,
                Title = "Dlaczego struktury danych są ważne?",
                Description = "Znaczenie struktur danych w wydajnym programowaniu.",
                XpReward = 40,
                Order = 2,
                IsActive = true,
                Steps = new List<LessonStep>
                {
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Efektywność",
                        Content = "Dobrze dobrana struktura danych może drastycznie przyspieszyć działanie programu, zredukować zużycie pamięci i pozwolić na prostszą implementację.",
                        Order = 1
                    },
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Przykład: stos",
                        Content = "Stos (stack) pozwala na szybki dostęp do ostatnio dodanego elementu – świetnie nadaje się do implementacji funkcjonalności cofania.",
                        Order = 2
                    },
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Przykład: kolejka",
                        Content = "Kolejka (queue) sprawdza się w systemach kolejkowania zadań, np. przetwarzanie wydruku w drukarkach.",
                        Order = 3
                    }
                }
            };

            var quizIntro = new Quiz
            {
                Id = Guid.NewGuid(),
                Module = moduleIntro,
                Title = "Quiz: Wprowadzenie do struktur danych",
                Description = "Sprawdź, czy rozumiesz czym są struktury danych i do czego służą.",
                XpReward = 25,
                TimeLimit = 5,
                IsActive = true,
                Questions = new List<QuizQuestion>()
            };

            var quizIntroQ1 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                Quiz = quizIntro,
                QuestionText = "Która z poniższych NIE jest strukturą danych?",
                Type = QuestionType.SingleChoice,
                Options = new List<QuizOption>
                {
                    new QuizOption { Id = Guid.NewGuid(), Text = "Tablica", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Algorytm sortowania", IsCorrect = true },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Stos", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Drzewo binarne", IsCorrect = false }
                }
            };
            var quizIntroQ2 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                Quiz = quizIntro,
                QuestionText = "Do czego służy stos (stack)?",
                Type = QuestionType.SingleChoice,
                Options = new List<QuizOption>
                {
                    new QuizOption { Id = Guid.NewGuid(), Text = "Do przechowywania elementów w kolejności FIFO", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Do przechowywania elementów w kolejności LIFO", IsCorrect = true },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Do wyszukiwania elementów w czasie O(log n)", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Do przechowywania par klucz-wartość", IsCorrect = false }
                }
            };
            var quizIntroQ3 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                Quiz = quizIntro,
                QuestionText = "Podaj przykład zastosowania kolejki (queue) w praktyce.",
                Type = QuestionType.SingleChoice,
                Options = new List<QuizOption>
                {
                    new QuizOption { Id = Guid.NewGuid(), Text = "Przechowywanie historii operacji (undo)", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Obsługa zadań drukowania w drukarce", IsCorrect = true },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Reprezentacja hierarchii w organizacji", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Przechowywanie par klucz-wartość", IsCorrect = false }
                }
            };
            quizIntro.Questions.AddRange(new[] { quizIntroQ1, quizIntroQ2, quizIntroQ3 });
            moduleIntro.Lessons = new List<Lesson> { lessonIntro1, lessonIntro2 };
            moduleIntro.Quizzes = new List<Quiz> { quizIntro };
            db.Modules.Add(moduleIntro);

            // --- MODULE 2: Tablice ---
            var moduleArrays = new Module
            {
                Id = Guid.NewGuid(),
                Title = "Tablice",
                Description = "Dowiedz się czym są tablice, jak je deklarować, inicjalizować i w jaki sposób efektywnie z nich korzystać.",
                IconUrl = "/modules/arrays.png",
                Order = 2,
                IsActive = true
            };

            var lessonArrays1 = new Lesson
            {
                Id = Guid.NewGuid(),
                Module = moduleArrays,
                Title = "Podstawy tablic",
                Description = "Definicja tablicy, deklarowanie i inicjalizacja.",
                XpReward = 50,
                Order = 1,
                IsActive = true,
                Steps = new List<LessonStep>
                {
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Definicja tablicy",
                        Content = "Tablica to zbiór elementów tego samego typu, ułożonych kolejno w pamięci. W C#: int[] liczby = new int[5];",
                        Order = 1,
                        CodeExample = "int[] liczby = new int[5];"
                    },
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Dostęp do elementów",
                        Content = "Aby odczytać lub ustawić wartość elementu, podaj jego indeks. Indeksy zaczynają się od 0.",
                        Order = 2,
                        CodeExample = "liczby[0] = 10;"
                    },
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Tablice wielowymiarowe",
                        Content = "Tablice mogą być wielowymiarowe, np. macierz 2D: int[,] macierz = new int[3,2];",
                        Order = 3,
                        CodeExample = "int[,] macierz = new int[3,2];"
                    }
                }
            };

            var lessonArrays2 = new Lesson
            {
                Id = Guid.NewGuid(),
                Module = moduleArrays,
                Title = "Zastosowania tablic",
                Description = "Gdzie i dlaczego warto używać tablic.",
                XpReward = 40,
                Order = 2,
                IsActive = true,
                Steps = new List<LessonStep>
                {
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Przechowywanie dużych ilości danych",
                        Content = "Tablice sprawdzają się do przechowywania dużych, jednorodnych zbiorów danych, np. ocen uczniów.",
                        Order = 1
                    },
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Podstawa innych struktur",
                        Content = "Tablice są fundamentem dla bardziej złożonych struktur danych, takich jak stosy, kolejki czy macierze.",
                        Order = 2
                    }
                }
            };

            var quizArrays = new Quiz
            {
                Id = Guid.NewGuid(),
                Module = moduleArrays,
                Title = "Quiz: Tablice",
                Description = "Zweryfikuj swoją wiedzę na temat tablic.",
                XpReward = 30,
                TimeLimit = 7,
                IsActive = true,
                Questions = new List<QuizQuestion>()
            };

            var quizArraysQ1 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                Quiz = quizArrays,
                QuestionText = "Jaki jest indeks pierwszego elementu tablicy w C#?",
                Type = QuestionType.SingleChoice,
                Options = new List<QuizOption>
                {
                    new QuizOption { Id = Guid.NewGuid(), Text = "0", IsCorrect = true },
                    new QuizOption { Id = Guid.NewGuid(), Text = "1", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "-1", IsCorrect = false }
                }
            };
            var quizArraysQ2 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                Quiz = quizArrays,
                QuestionText = "Jak zadeklarować i zainicjalizować tablicę pięciu liczb całkowitych w C#?",
                Type = QuestionType.SingleChoice,
                Options = new List<QuizOption>
                {
                    new QuizOption { Id = Guid.NewGuid(), Text = "int[5] tablica = new int[];", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "int[] tablica = new int[5];", IsCorrect = true },
                    new QuizOption { Id = Guid.NewGuid(), Text = "array tablica = int[5];", IsCorrect = false }
                }
            };
            var quizArraysQ3 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                Quiz = quizArrays,
                QuestionText = "Które operacje na tablicy są bardzo szybkie (O(1))?",
                Type = QuestionType.MultipleChoice,
                Options = new List<QuizOption>
                {
                    new QuizOption { Id = Guid.NewGuid(), Text = "Dostęp do elementu po indeksie", IsCorrect = true },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Dodanie elementu na koniec tablicy (statycznej)", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Modyfikacja elementu po indeksie", IsCorrect = true },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Usunięcie elementu ze środka tablicy", IsCorrect = false }
                }
            };
            quizArrays.Questions.AddRange(new[] { quizArraysQ1, quizArraysQ2, quizArraysQ3 });
            moduleArrays.Lessons = new List<Lesson> { lessonArrays1, lessonArrays2 };
            moduleArrays.Quizzes = new List<Quiz> { quizArrays };
            db.Modules.Add(moduleArrays);

            // --- MODULE 3: Listy ---
            var moduleLists = new Module
            {
                Id = Guid.NewGuid(),
                Title = "Listy",
                Description = "Poznaj dynamiczne listy, ich rodzaje (jednokierunkowe, dwukierunkowe), zalety i wady oraz praktyczne zastosowania.",
                IconUrl = "/modules/lists.png",
                Order = 3,
                IsActive = true
            };

            var lessonLists1 = new Lesson
            {
                Id = Guid.NewGuid(),
                Module = moduleLists,
                Title = "Podstawy list",
                Description = "Czym jest lista i czym różni się od tablicy.",
                XpReward = 45,
                Order = 1,
                IsActive = true,
                Steps = new List<LessonStep>
                {
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Definicja listy",
                        Content = "Lista to dynamiczna struktura danych, która pozwala na łatwe dodawanie i usuwanie elementów.",
                        Order = 1
                    },
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Różnica względem tablicy",
                        Content = "Lista może zmieniać rozmiar w trakcie działania programu, w przeciwieństwie do statycznej tablicy.",
                        Order = 2
                    }
                }
            };

            var lessonLists2 = new Lesson
            {
                Id = Guid.NewGuid(),
                Module = moduleLists,
                Title = "Listy jednokierunkowe i dwukierunkowe",
                Description = "Czym różnią się listy jednokierunkowe i dwukierunkowe?",
                XpReward = 45,
                Order = 2,
                IsActive = true,
                Steps = new List<LessonStep>
                {
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Lista jednokierunkowa",
                        Content = "Składa się z węzłów, z których każdy zawiera wartość oraz wskaźnik na kolejny element.",
                        Order = 1
                    },
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Lista dwukierunkowa",
                        Content = "Każdy węzeł przechowuje wskaźniki na element następny i poprzedni, co umożliwia łatwe przechodzenie w obie strony.",
                        Order = 2
                    }
                }
            };

            var lessonLists3 = new Lesson
            {
                Id = Guid.NewGuid(),
                Module = moduleLists,
                Title = "Zastosowania list",
                Description = "Gdzie warto używać list zamiast tablic?",
                XpReward = 45,
                Order = 3,
                IsActive = true,
                Steps = new List<LessonStep>
                {
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Dynamiczne kolekcje",
                        Content = "Listy są używane tam, gdzie często dodajemy lub usuwamy elementy w różnych miejscach kolekcji.",
                        Order = 1
                    },
                    new LessonStep
                    {
                        Id = Guid.NewGuid(),
                        Title = "Przykłady zastosowań",
                        Content = "Edytory tekstu (linie dokumentu), implementacje stosów i kolejek, algorytmy grafowe.",
                        Order = 2
                    }
                }
            };

            var quizLists = new Quiz
            {
                Id = Guid.NewGuid(),
                Module = moduleLists,
                Title = "Quiz: Listy",
                Description = "Sprawdź swoją wiedzę na temat list.",
                XpReward = 30,
                TimeLimit = 8,
                IsActive = true,
                Questions = new List<QuizQuestion>()
            };

            var quizListsQ1 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                Quiz = quizLists,
                QuestionText = "Jaką złożoność czasową ma dodanie elementu na początek listy jednokierunkowej?",
                Type = QuestionType.SingleChoice,
                Options = new List<QuizOption>
                {
                    new QuizOption { Id = Guid.NewGuid(), Text = "O(1)", IsCorrect = true },
                    new QuizOption { Id = Guid.NewGuid(), Text = "O(n)", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "O(log n)", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "O(n^2)", IsCorrect = false }
                }
            };
            var quizListsQ2 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                Quiz = quizLists,
                QuestionText = "Która ze struktur pozwala na łatwe przechodzenie w obie strony?",
                Type = QuestionType.SingleChoice,
                Options = new List<QuizOption>
                {
                    new QuizOption { Id = Guid.NewGuid(), Text = "Lista jednokierunkowa", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Lista dwukierunkowa", IsCorrect = true },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Tablica", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Stos", IsCorrect = false }
                }
            };
            var quizListsQ3 = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                Quiz = quizLists,
                QuestionText = "W jakiej sytuacji lepiej użyć listy niż tablicy?",
                Type = QuestionType.MultipleChoice,
                Options = new List<QuizOption>
                {
                    new QuizOption { Id = Guid.NewGuid(), Text = "Chcesz często dodawać elementy w środku kolekcji", IsCorrect = true },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Wiesz z góry ile będzie elementów", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Potrzebujesz szybkiego dostępu po indeksie", IsCorrect = false },
                    new QuizOption { Id = Guid.NewGuid(), Text = "Rozmiar kolekcji może się zmieniać dynamicznie", IsCorrect = true }
                }
            };
            quizLists.Questions.AddRange(new[] { quizListsQ1, quizListsQ2, quizListsQ3 });
            moduleLists.Lessons = new List<Lesson> { lessonLists1, lessonLists2, lessonLists3 };
            moduleLists.Quizzes = new List<Quiz> { quizLists };
            db.Modules.Add(moduleLists);

            db.SaveChanges();
        }
    }
}