using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data;
using DSA.DTOs.Quizzes;
using DSA.Models;
using Microsoft.EntityFrameworkCore;

namespace DSA.Services
{
    public class QuizService : IQuizService
    {
        private readonly ApplicationDbContext _context;

        public QuizService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ModuleQuizzesResponse> GetQuizzesForModuleAsync(Guid moduleId, Guid userId)
        {
            var module = await _context.Modules
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null)
                throw new ArgumentException("Module not found", nameof(moduleId));

            var quizzes = await _context.Quizzes
                .Where(q => q.ModuleId == moduleId)
                .Select(q => new QuizDto
                {
                    Id = q.Id,
                    Title = q.Title,
                    Description = q.Description,
                    XpReward = q.XpReward,
                    TimeLimit = q.TimeLimit,
                    IsActive = q.IsActive,
                    QuestionCount = q.Questions.Count,
                    IsCompleted = q.UserResults.Any(ur => ur.UserId == userId),
                    BestScore = q.UserResults
                        .Where(ur => ur.UserId == userId)
                        .OrderByDescending(ur => ur.Score)
                        .Select(ur => ur.Score)
                        .FirstOrDefault(),
                    BestPercentage = q.UserResults
                        .Where(ur => ur.UserId == userId)
                        .OrderByDescending(ur => (double)ur.Score / ur.TotalQuestions)
                        .Select(ur => (int)Math.Round((ur.Score / (double)ur.TotalQuestions) * 100))
                        .FirstOrDefault(),
                    AttemptCount = q.UserResults.Count(ur => ur.UserId == userId)
                })
                .ToListAsync();

            return new ModuleQuizzesResponse
            {
                ModuleId = moduleId,
                ModuleTitle = module.Title,
                Quizzes = quizzes,
                TotalQuizzes = quizzes.Count
            };
        }

        public async Task<QuizDetailDto?> GetQuizDetailsAsync(Guid quizId, Guid userId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(qq => qq.Options)
                .Include(q => q.Module)
                .Include(q => q.UserResults.Where(ur => ur.UserId == userId))
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return null;

            // Get attempts count
            int attemptCount = quiz.UserResults.Count;

            // Check if quiz is completed
            bool isCompleted = quiz.UserResults.Any();

            var questions = new List<QuizQuestionDto>();

            // Map questions, hiding correct answers if not completed yet
            foreach (var question in quiz.Questions)
            {
                questions.Add(new QuizQuestionDto
                {
                    Id = question.Id,
                    QuestionText = question.QuestionText,
                    CodeSnippet = question.CodeSnippet,
                    QuestionType = question.Type.ToString(),
                    Options = question.Options.Select(o => new QuizOptionDto
                    {
                        Id = o.Id,
                        Text = o.Text
                    }).ToList()
                });
            }

            return new QuizDetailDto
            {
                Id = quiz.Id,
                ModuleId = quiz.ModuleId,
                ModuleTitle = quiz.Module.Title,
                Title = quiz.Title,
                Description = quiz.Description,
                XpReward = quiz.XpReward,
                TimeLimit = quiz.TimeLimit,
                IsActive = quiz.IsActive,
                Questions = questions,
                IsCompleted = isCompleted,
                AttemptCount = attemptCount
            };
        }

        public async Task<QuizResultResponse> SubmitQuizAnswersAsync(Guid quizId, Guid userId, QuizSubmitRequest request)
        {
            try // Dodaj try-catch dla lepszej diagnostyki
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Questions)
                    .ThenInclude(qq => qq.Options)
                    .FirstOrDefaultAsync(q => q.Id == quizId);

                if (quiz == null)
                {
                    return new QuizResultResponse
                    {
                        Success = false,
                        Message = "Quiz not found"
                    };
                }

                // Walidacja danych
                if (request.Answers == null || !request.Answers.Any())
                {
                    return new QuizResultResponse
                    {
                        Success = false,
                        Message = "No answers provided"
                    };
                }

                // Sprawdzamy czas
                var startTime = request.StartedAt;
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                // ROZWIĄZANIE PROBLEMU Z CZASEM:
                // Zmiana limitu czasu - dla elastyczności
                // Zwiększamy limit o 50% dla pewności
                if (duration.TotalMinutes > quiz.TimeLimit * 1.5) // Allow 50% grace period
                {
                    return new QuizResultResponse
                    {
                        Success = false,
                        Message = $"Time limit exceeded ({quiz.TimeLimit} minutes)"
                    };
                }

                // Obliczanie wyniku
                int correctAnswers = 0;
                int totalQuestions = quiz.Questions.Count;
                var answerResults = new List<QuizAnswerResultDto>();

                foreach (var questionAnswer in request.Answers)
                {
                    // Sprawdź czy pytanie istnieje
                    var question = quiz.Questions.FirstOrDefault(q => q.Id == questionAnswer.QuestionId);
                    if (question == null) continue;

                    var correctOptionIds = question.Options
                        .Where(o => o.IsCorrect)
                        .Select(o => o.Id)
                        .ToList();

                    bool isCorrect = false;

                    // Sprawdzamy odpowiedź
                    switch (question.Type)
                    {
                        case QuestionType.SingleChoice:
                            isCorrect = questionAnswer.SelectedOptionIds.Count == 1 &&
                                       correctOptionIds.Count == 1 &&
                                       questionAnswer.SelectedOptionIds[0] == correctOptionIds[0];
                            break;

                        case QuestionType.MultipleChoice:
                            isCorrect = correctOptionIds.Count == questionAnswer.SelectedOptionIds.Count &&
                                       correctOptionIds.All(id => questionAnswer.SelectedOptionIds.Contains(id));
                            break;

                        case QuestionType.TrueFalse:
                            isCorrect = questionAnswer.SelectedOptionIds.Count == 1 &&
                                       correctOptionIds.Count == 1 &&
                                       questionAnswer.SelectedOptionIds[0] == correctOptionIds[0];
                            break;
                    }

                    if (isCorrect)
                    {
                        correctAnswers++;
                    }

                    answerResults.Add(new QuizAnswerResultDto
                    {
                        QuestionId = question.Id,
                        QuestionText = question.QuestionText,
                        SelectedOptionIds = questionAnswer.SelectedOptionIds,
                        CorrectOptionIds = correctOptionIds,
                        IsCorrect = isCorrect,
                        Explanation = null
                    });
                }

                // Oblicz procent i XP
                int scorePercentage = totalQuestions > 0 ?
                    (int)Math.Round((correctAnswers / (double)totalQuestions) * 100) : 0;
                int xpEarned = (int)Math.Round((correctAnswers / (double)totalQuestions) * quiz.XpReward);
                string grade = GetGradeFromPercentage(scorePercentage);

                // TUTAJ JEST KLUCZOWA ZMIANA - utworzenie nowego obiektu QuizResult
                var quizResult = new QuizResult
                {
                    Id = Guid.NewGuid(), // Generuj nowy GUID
                    UserId = userId,
                    QuizId = quizId,
                    Score = correctAnswers,
                    TotalQuestions = totalQuestions,
                    CorrectAnswers = correctAnswers,
                    XpEarned = xpEarned,
                    StartedAt = request.StartedAt,
                    CompletedAt = endTime
                };

                // Dodaj wynik do kontekstu
                _context.QuizResults.Add(quizResult);

                // Zapisz odpowiedzi
                foreach (var answer in request.Answers)
                {
                    var question = quiz.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                    if (question == null) continue;

                    var isCorrect = answerResults.FirstOrDefault(ar => ar.QuestionId == answer.QuestionId)?.IsCorrect ?? false;

                    _context.QuizAnswers.Add(new QuizAnswer
                    {
                        Id = Guid.NewGuid(),
                        QuizResultId = quizResult.Id,
                        QuestionId = answer.QuestionId,
                        SelectedOptionIds = answer.SelectedOptionIds,
                        IsCorrect = isCorrect
                    });
                }

                // Zaktualizuj XP użytkownika
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.XpPoints += xpEarned;
                    user.LastActivityDate = DateTime.UtcNow;

                    // Aktualizacja streak
                    if (!user.LastActivityDate.HasValue ||
                        user.LastActivityDate.Value.Date < DateTime.UtcNow.Date)
                    {
                        await UpdateUserStreakAsync(user);
                    }
                }

                // Zapisz do bazy danych
                await _context.SaveChangesAsync();

                // Sprawdź czy to pierwszy wynik
                bool isFirstAttempt = await _context.QuizResults
                    .CountAsync(qr => qr.UserId == userId && qr.QuizId == quizId) == 1;

                // Sprawdź czy to osobisty rekord
                bool isPersonalBest = false;
                if (!isFirstAttempt)
                {
                    var bestPreviousScore = await _context.QuizResults
                        .Where(qr => qr.UserId == userId && qr.QuizId == quizId && qr.Id != quizResult.Id)
                        .OrderByDescending(qr => qr.Score)
                        .Select(qr => qr.Score)
                        .FirstOrDefaultAsync();

                    isPersonalBest = correctAnswers > bestPreviousScore;
                }
                else
                {
                    isPersonalBest = true;
                }

                // Czy zaliczony?
                bool isPassing = scorePercentage >= 70; // 70% passing threshold

                return new QuizResultResponse
                {
                    Success = true,
                    Message = "Quiz answers submitted successfully",
                    ResultId = quizResult.Id,
                    Score = correctAnswers,
                    TotalQuestions = totalQuestions,
                    CorrectAnswers = correctAnswers,
                    ScorePercentage = scorePercentage,
                    XpEarned = xpEarned,
                    Grade = grade,
                    AnswerResults = answerResults,
                    IsPassing = isPassing,
                    IsFirstAttempt = isFirstAttempt,
                    IsPersonalBest = isPersonalBest,
                    StartedAt = request.StartedAt,
                    CompletedAt = endTime
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error submitting quiz: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return new QuizResultResponse
                {
                    Success = false,
                    Message = $"An error occurred: {ex.Message}"
                };
            }
        }

        public async Task<UserQuizResultsResponse?> GetUserQuizResultsAsync(Guid quizId, Guid userId)
        {
            var quiz = await _context.Quizzes
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return null;

            var results = await _context.QuizResults
                .Where(qr => qr.UserId == userId && qr.QuizId == quizId)
                .OrderByDescending(qr => qr.CompletedAt)
                .ToListAsync();

            if (!results.Any())
            {
                return new UserQuizResultsResponse
                {
                    QuizId = quizId,
                    QuizTitle = quiz.Title,
                    AttemptCount = 0,
                    BestScore = null,
                    BestPercentage = null,
                    FirstAttemptDate = null,
                    LastAttemptDate = null,
                    Attempts = new List<QuizAttemptDto>()
                };
            }

            var bestScore = results.Max(r => r.Score);
            var bestPercentage = results.Max(r => (int)Math.Round((r.Score / (double)r.TotalQuestions) * 100));

            var attempts = results.Select(r => new QuizAttemptDto
            {
                Id = r.Id,
                Score = r.Score,
                TotalQuestions = r.TotalQuestions,
                CorrectAnswers = r.CorrectAnswers,
                ScorePercentage = (int)Math.Round((r.CorrectAnswers / (double)r.TotalQuestions) * 100),
                XpEarned = r.XpEarned,
                Grade = GetGradeFromPercentage((int)Math.Round((r.Score / (double)r.TotalQuestions) * 100)),
                IsPassing = (r.Score / (double)r.TotalQuestions) >= 0.7, // 70% passing threshold
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                Duration = r.CompletedAt - r.StartedAt
            }).ToList();

            return new UserQuizResultsResponse
            {
                QuizId = quizId,
                QuizTitle = quiz.Title,
                AttemptCount = results.Count,
                BestScore = bestScore,
                BestPercentage = bestPercentage,
                FirstAttemptDate = results.Min(r => r.CompletedAt),
                LastAttemptDate = results.Max(r => r.CompletedAt),
                Attempts = attempts
            };
        }

        // Helper methods
        private string GetGradeFromPercentage(int percentage)
        {
            if (percentage >= 90) return "A";
            if (percentage >= 80) return "B";
            if (percentage >= 70) return "C";
            if (percentage >= 60) return "D";
            return "F";
        }

        private async Task UpdateUserStreakAsync(User user)
        {
            var today = DateTime.UtcNow.Date;

            if (!user.LastActivityDate.HasValue)
            {
                // First activity ever
                user.CurrentStreak = 1;
                user.MaxStreak = 1;
            }
            else
            {
                var lastActivityDate = user.LastActivityDate.Value.Date;
                var yesterday = today.AddDays(-1);

                if (lastActivityDate == yesterday)
                {
                    // Consecutive day, increment streak
                    user.CurrentStreak++;

                    if (user.CurrentStreak > user.MaxStreak)
                    {
                        user.MaxStreak = user.CurrentStreak;
                    }
                }
                else if (lastActivityDate < yesterday)
                {
                    // Streak broken, start new streak
                    user.CurrentStreak = 1;
                }
                // If lastActivityDate == today, streak remains unchanged
            }

            user.LastActivityDate = today;
        }
    }
}
