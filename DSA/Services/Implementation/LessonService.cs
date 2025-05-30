using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data;
using DSA.DTOs.Lessons;
using DSA.Models;
using Microsoft.EntityFrameworkCore;

namespace DSA.Services
{
    public class LessonService : ILessonService
    {
        private readonly ApplicationDbContext _context;

        public LessonService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ModulesResponse> GetModulesAsync(Guid userId)
        {
            var modules = await _context.Modules
                .OrderBy(m => m.Order)
                .Select(m => new ModuleDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    IconUrl = m.IconUrl,
                    Order = m.Order,
                    IsActive = m.IsActive,
                    LessonCount = m.Lessons.Count,
                    QuizCount = m.Quizzes.Count,
                    CompletedLessonCount = m.Lessons.Count(l =>
                        l.UserProgresses.Any(up =>
                            up.UserId == userId && up.IsCompleted)),
                    CompletedQuizCount = m.Quizzes.Count(q =>
                        q.UserResults.Any(ur =>
                            ur.UserId == userId)),
                    IsCompleted =
                        m.Lessons.All(l =>
                            l.UserProgresses.Any(up =>
                                up.UserId == userId && up.IsCompleted))
                        && m.Quizzes.All(q =>
                            q.UserResults.Any(ur =>
                                ur.UserId == userId))
                })
                .ToListAsync();

            // Calculate progress percentage
            foreach (var module in modules)
            {
                int total = module.LessonCount + module.QuizCount;
                if (total > 0)
                {
                    int completed = module.CompletedLessonCount + module.CompletedQuizCount;
                    module.ProgressPercentage = (int)Math.Round((completed / (double)total) * 100);
                }
                else
                {
                    module.ProgressPercentage = 0;
                }
            }

            return new ModulesResponse
            {
                Modules = modules,
                TotalModules = modules.Count
            };
        }

        public async Task<ModuleDetailDto?> GetModuleDetailsAsync(Guid moduleId, Guid userId)
        {
            var module = await _context.Modules
                .Include(m => m.Lessons)
                .Include(m => m.Quizzes)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null)
                return null;

            var result = new ModuleDetailDto
            {
                Id = module.Id,
                Title = module.Title,
                Description = module.Description,
                IconUrl = module.IconUrl,
                Order = module.Order,
                IsActive = module.IsActive
            };

            // Get lessons with progress
            var lessonSummaries = new List<LessonSummaryDto>();

            foreach (var lesson in module.Lessons.OrderBy(l => l.Order))
            {
                var userProgress = await _context.UserProgresses
                    .Include(up => up.StepProgresses)
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lesson.Id);

                int stepCount = await _context.LessonSteps
                    .CountAsync(ls => ls.LessonId == lesson.Id);

                int completedStepCount = userProgress?.StepProgresses
                    .Count(sp => sp.IsCompleted) ?? 0;

                int progressPercentage = stepCount > 0 ?
                    (int)Math.Round((completedStepCount / (double)stepCount) * 100) : 0;

                lessonSummaries.Add(new LessonSummaryDto
                {
                    Id = lesson.Id,
                    Title = lesson.Title,
                    Description = lesson.Description,
                    Order = lesson.Order,
                    XpReward = lesson.XpReward,
                    IsCompleted = userProgress?.IsCompleted ?? false,
                    IsActive = lesson.IsActive,
                    StepCount = stepCount,
                    CompleteStepCount = completedStepCount,
                    ProgressPercentage = progressPercentage
                });
            }

            result.Lessons = lessonSummaries;

            // Get quizzes with results
            var quizSummaries = new List<QuizSummaryDto>();

            foreach (var quiz in module.Quizzes)
            {
                var bestResult = await _context.QuizResults
                    .Where(qr => qr.UserId == userId && qr.QuizId == quiz.Id)
                    .OrderByDescending(qr => qr.Score)
                    .FirstOrDefaultAsync();

                int questionCount = await _context.QuizQuestions
                    .CountAsync(qq => qq.QuizId == quiz.Id);

                quizSummaries.Add(new QuizSummaryDto
                {
                    Id = quiz.Id,
                    Title = quiz.Title,
                    Description = quiz.Description,
                    XpReward = quiz.XpReward,
                    TimeLimit = quiz.TimeLimit,
                    IsCompleted = bestResult != null,
                    BestScore = bestResult?.Score,
                    QuestionCount = questionCount
                });
            }

            result.Quizzes = quizSummaries;

            // Calculate overall progress
            int totalItems = module.Lessons.Count + module.Quizzes.Count;
            int completedItems = lessonSummaries.Count(ls => ls.IsCompleted) +
                                quizSummaries.Count(qs => qs.IsCompleted);

            result.ProgressPercentage = totalItems > 0 ?
                (int)Math.Round((completedItems / (double)totalItems) * 100) : 0;

            result.IsCompleted = totalItems > 0 && completedItems == totalItems;

            // Get next module if exists
            var nextModule = await _context.Modules
                .Where(m => m.Order > module.Order)
                .OrderBy(m => m.Order)
                .FirstOrDefaultAsync();

            if (nextModule != null)
            {
                result.NextModule = new ModuleDependencyDto
                {
                    Id = nextModule.Id,
                    Title = nextModule.Title,
                    IsActive = nextModule.IsActive,
                    Order = nextModule.Order
                };
            }

            return result;
        }

        public async Task<LessonsResponse> GetLessonsInModuleAsync(Guid moduleId, Guid userId)
        {
            var module = await _context.Modules
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null)
                throw new ArgumentException("Module not found", nameof(moduleId));

            var lessons = await _context.Lessons
                .Where(l => l.ModuleId == moduleId)
                .OrderBy(l => l.Order)
                .Select(l => new LessonDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    Description = l.Description,
                    Order = l.Order,
                    XpReward = l.XpReward,
                    IsActive = l.IsActive,
                    IsCompleted = l.UserProgresses.Any(up => up.UserId == userId && up.IsCompleted),
                    StepCount = l.Steps.Count,
                    CompletedStepCount = l.Steps.Count(ls =>
                        ls.StepProgresses.Any(sp =>
                            sp.UserProgress.UserId == userId && sp.IsCompleted))
                })
                .ToListAsync();

            // Calculate progress percentage for each lesson
            foreach (var lesson in lessons)
            {
                if (lesson.StepCount > 0)
                {
                    lesson.ProgressPercentage = (int)Math.Round((lesson.CompletedStepCount / (double)lesson.StepCount) * 100);
                }
                else
                {
                    lesson.ProgressPercentage = 0;
                }
            }

            return new LessonsResponse
            {
                ModuleId = moduleId,
                ModuleTitle = module.Title,
                Lessons = lessons,
                TotalLessons = lessons.Count
            };
        }

        public async Task<LessonDetailDto?> GetLessonDetailsAsync(Guid lessonId, Guid userId)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                return null;

            // Get user progress
            var progress = await GetLessonProgressAsync(lessonId, userId);

            // Get previous lesson (in same module)
            var previousLesson = await _context.Lessons
                .Where(l => l.ModuleId == lesson.ModuleId && l.Order < lesson.Order)
                .OrderByDescending(l => l.Order)
                .FirstOrDefaultAsync();

            // Get next lesson (in same module)
            var nextLesson = await _context.Lessons
                .Where(l => l.ModuleId == lesson.ModuleId && l.Order > lesson.Order)
                .OrderBy(l => l.Order)
                .FirstOrDefaultAsync();

            return new LessonDetailDto
            {
                Id = lesson.Id,
                ModuleId = lesson.ModuleId,
                ModuleTitle = lesson.Module.Title,
                Title = lesson.Title,
                Description = lesson.Description,
                Order = lesson.Order,
                XpReward = lesson.XpReward,
                IsActive = lesson.IsActive,
                IsCompleted = progress?.IsCompleted ?? false,
                Progress = progress ?? new LessonProgressDto { LessonId = lessonId },
                PreviousLesson = previousLesson != null ? new LessonDependencyDto
                {
                    Id = previousLesson.Id,
                    Title = previousLesson.Title,
                    IsActive = previousLesson.IsActive,
                    Order = previousLesson.Order,
                    ModuleId = previousLesson.ModuleId
                } : null,
                NextLesson = nextLesson != null ? new LessonDependencyDto
                {
                    Id = nextLesson.Id,
                    Title = nextLesson.Title,
                    IsActive = nextLesson.IsActive,
                    Order = nextLesson.Order,
                    ModuleId = nextLesson.ModuleId
                } : null
            };
        }

        public async Task<LessonStepsResponse?> GetLessonStepsAsync(Guid lessonId, Guid userId)
        {
            var lesson = await _context.Lessons
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                return null;

            // Get user progress for this lesson
            var userProgress = await _context.UserProgresses
                .Include(up => up.StepProgresses)
                .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lessonId);

            // If no progress record exists, create one
            if (userProgress == null)
            {
                userProgress = new UserProgress
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    LessonId = lessonId,
                    IsCompleted = false,
                    StartedAt = DateTime.UtcNow
                };

                _context.UserProgresses.Add(userProgress);
                await _context.SaveChangesAsync();
            }

            // Get all steps for the lesson
            var steps = await _context.LessonSteps
                .Where(ls => ls.LessonId == lessonId)
                .OrderBy(ls => ls.Order)
                .ToListAsync();

            // Map steps to DTOs with completion status
            var stepDtos = new List<LessonStepDto>();

            foreach (var step in steps)
            {
                var stepProgress = userProgress.StepProgresses
                    .FirstOrDefault(sp => sp.LessonStepId == step.Id);

                stepDtos.Add(new LessonStepDto
                {
                    Id = step.Id,
                    Title = step.Title,
                    Content = step.Content,
                    CodeExample = step.CodeExample,
                    Order = step.Order,
                    IsCompleted = stepProgress?.IsCompleted ?? false,
                    CompletedAt = stepProgress?.CompletedAt
                });
            }

            // Calculate progress
            int totalSteps = steps.Count;
            int completedSteps = stepDtos.Count(s => s.IsCompleted);
            int progressPercentage = totalSteps > 0 ?
                (int)Math.Round((completedSteps / (double)totalSteps) * 100) : 0;

            return new LessonStepsResponse
            {
                LessonId = lessonId,
                LessonTitle = lesson.Title,
                Steps = stepDtos,
                TotalSteps = totalSteps,
                CompletedSteps = completedSteps,
                ProgressPercentage = progressPercentage
            };
        }

        public async Task<StepCompleteResponse> CompleteStepAsync(Guid lessonId, Guid stepId, Guid userId)
        {
            // Validate lesson and step exist
            var step = await _context.LessonSteps
                .Include(ls => ls.Lesson)
                .FirstOrDefaultAsync(ls => ls.Id == stepId && ls.LessonId == lessonId);

            if (step == null)
            {
                return new StepCompleteResponse
                {
                    Success = false,
                    Message = "Lesson step not found"
                };
            }

            // Get or create user progress
            var userProgress = await _context.UserProgresses
                .Include(up => up.StepProgresses)
                .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lessonId);

            if (userProgress == null)
            {
                userProgress = new UserProgress
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    LessonId = lessonId,
                    IsCompleted = false,
                    StartedAt = DateTime.UtcNow,
                    StepProgresses = new List<StepProgress>()
                };

                _context.UserProgresses.Add(userProgress);
            }

            // Get or create step progress
            var stepProgress = userProgress.StepProgresses
                .FirstOrDefault(sp => sp.LessonStepId == stepId);

            if (stepProgress == null)
            {
                stepProgress = new StepProgress
                {
                    Id = Guid.NewGuid(),
                    UserProgressId = userProgress.Id,
                    LessonStepId = stepId,
                    IsCompleted = false
                };

                userProgress.StepProgresses.Add(stepProgress);
            }

            // If already completed, return early
            if (stepProgress.IsCompleted)
            {
                return new StepCompleteResponse
                {
                    Success = true,
                    Message = "Step already completed",
                    LessonCompleted = userProgress.IsCompleted,
                    XpEarned = 0,
                    Step = new LessonStepDto
                    {
                        Id = step.Id,
                        Title = step.Title,
                        Content = step.Content,
                        CodeExample = step.CodeExample,
                        Order = step.Order,
                        IsCompleted = true,
                        CompletedAt = stepProgress.CompletedAt
                    }
                };
            }

            // Mark step as completed
            stepProgress.IsCompleted = true;
            stepProgress.CompletedAt = DateTime.UtcNow;

            // Check if lesson is now completed
            int totalSteps = await _context.LessonSteps
                .CountAsync(ls => ls.LessonId == lessonId);

            int completedSteps = userProgress.StepProgresses
                .Count(sp => sp.IsCompleted);

            bool lessonCompleted = completedSteps >= totalSteps;
            int xpEarned = 0;

            // If all steps are completed, mark lesson as completed
            if (lessonCompleted && !userProgress.IsCompleted)
            {
                userProgress.IsCompleted = true;
                userProgress.CompletedAt = DateTime.UtcNow;

                // Award XP for lesson completion
                xpEarned = step.Lesson.XpReward;

                // Update user XP
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.XpPoints += xpEarned;
                    user.LastActivityDate = DateTime.UtcNow;

                    // Update streak if needed
                    if (!user.LastActivityDate.HasValue ||
                        user.LastActivityDate.Value.Date < DateTime.UtcNow.Date)
                    {
                        await UpdateUserStreakAsync(user);
                    }
                }
            }

            await _context.SaveChangesAsync();

            return new StepCompleteResponse
            {
                Success = true,
                Message = "Step completed successfully",
                LessonCompleted = lessonCompleted,
                XpEarned = xpEarned,
                Step = new LessonStepDto
                {
                    Id = step.Id,
                    Title = step.Title,
                    Content = step.Content,
                    CodeExample = step.CodeExample,
                    Order = step.Order,
                    IsCompleted = true,
                    CompletedAt = stepProgress.CompletedAt
                }
            };
        }

        public async Task<LessonCompleteResponse> CompleteLessonAsync(Guid lessonId, Guid userId)
        {
            // Validate lesson exists
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return new LessonCompleteResponse
                {
                    Success = false,
                    Message = "Lesson not found"
                };
            }

            // Check if all steps are completed
            var userProgress = await _context.UserProgresses
                .Include(up => up.StepProgresses)
                .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lessonId);

            if (userProgress == null)
            {
                return new LessonCompleteResponse
                {
                    Success = false,
                    Message = "You need to start and complete all steps in this lesson first"
                };
            }

            int totalSteps = await _context.LessonSteps
                .CountAsync(ls => ls.LessonId == lessonId);

            int completedSteps = userProgress.StepProgresses
                .Count(sp => sp.IsCompleted);

            if (completedSteps < totalSteps)
            {
                return new LessonCompleteResponse
                {
                    Success = false,
                    Message = $"You need to complete all steps first ({completedSteps}/{totalSteps})"
                };
            }

            // If already completed, return early
            if (userProgress.IsCompleted)
            {
                return new LessonCompleteResponse
                {
                    Success = true,
                    Message = "Lesson already completed",
                    TotalXpEarned = 0
                };
            }

            // Mark lesson as completed
            userProgress.IsCompleted = true;
            userProgress.CompletedAt = DateTime.UtcNow;

            // Award XP
            int xpEarned = lesson.XpReward;

            // Update user XP
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.XpPoints += xpEarned;
                user.LastActivityDate = DateTime.UtcNow;

                // Update streak if needed
                if (!user.LastActivityDate.HasValue ||
                    user.LastActivityDate.Value.Date < DateTime.UtcNow.Date)
                {
                    await UpdateUserStreakAsync(user);
                }
            }

            // Check if module is completed
            var moduleCompleted = await IsModuleCompletedAsync(lesson.ModuleId, userId);

            // Get next lesson or quiz if any
            Guid? nextLessonId = null;
            Guid? nextQuizId = null;

            // Find next lesson in the same module
            var nextLesson = await _context.Lessons
                .Where(l => l.ModuleId == lesson.ModuleId && l.Order > lesson.Order)
                .OrderBy(l => l.Order)
                .FirstOrDefaultAsync();

            if (nextLesson != null)
            {
                nextLessonId = nextLesson.Id;
            }
            else
            {
                // If no next lesson, find a quiz in this module that's not completed
                var nextQuiz = await _context.Quizzes
                    .Where(q => q.ModuleId == lesson.ModuleId &&
                           !q.UserResults.Any(ur => ur.UserId == userId))
                    .FirstOrDefaultAsync();

                if (nextQuiz != null)
                {
                    nextQuizId = nextQuiz.Id;
                }
            }

            await _context.SaveChangesAsync();

            return new LessonCompleteResponse
            {
                Success = true,
                Message = "Lesson completed successfully",
                TotalXpEarned = xpEarned,
                ModuleCompleted = moduleCompleted,
                NextLessonId = nextLessonId,
                NextQuizId = nextQuizId
            };
        }

        public async Task<LessonProgressDto?> GetLessonProgressAsync(Guid lessonId, Guid userId)
        {
            // Check if lesson exists
            var lesson = await _context.Lessons
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                return null;

            // Get user progress for this lesson
            var userProgress = await _context.UserProgresses
                .Include(up => up.StepProgresses)
                .FirstOrDefaultAsync(up => up.UserId == userId && up.LessonId == lessonId);

            // If no progress record exists yet
            if (userProgress == null)
            {
                return new LessonProgressDto
                {
                    LessonId = lessonId,
                    IsStarted = false,
                    IsCompleted = false,
                    StepCount = await _context.LessonSteps.CountAsync(ls => ls.LessonId == lessonId),
                    CompletedStepCount = 0,
                    ProgressPercentage = 0,
                    StepProgresses = new List<StepProgressDto>()
                };
            }

            // Get all steps for this lesson
            var steps = await _context.LessonSteps
                .Where(ls => ls.LessonId == lessonId)
                .ToListAsync();

            // Calculate progress
            int totalSteps = steps.Count;
            int completedSteps = userProgress.StepProgresses.Count(sp => sp.IsCompleted);
            int progressPercentage = totalSteps > 0 ?
                (int)Math.Round((completedSteps / (double)totalSteps) * 100) : 0;

            // Create step progress DTOs
            var stepProgressDtos = new List<StepProgressDto>();

            foreach (var step in steps)
            {
                var stepProgress = userProgress.StepProgresses
                    .FirstOrDefault(sp => sp.LessonStepId == step.Id);

                stepProgressDtos.Add(new StepProgressDto
                {
                    StepId = step.Id,
                    IsCompleted = stepProgress?.IsCompleted ?? false,
                    CompletedAt = stepProgress?.CompletedAt
                });
            }

            return new LessonProgressDto
            {
                LessonId = lessonId,
                IsStarted = true,
                IsCompleted = userProgress.IsCompleted,
                StartedAt = userProgress.StartedAt,
                CompletedAt = userProgress.CompletedAt,
                StepCount = totalSteps,
                CompletedStepCount = completedSteps,
                ProgressPercentage = progressPercentage,
                StepProgresses = stepProgressDtos
            };
        }

        public async Task<UserProgressResponse> GetUserProgressAsync(Guid userId)
        {
            // Get all modules and their lessons
            var modules = await _context.Modules
                .Include(m => m.Lessons)
                .OrderBy(m => m.Order)
                .ToListAsync();

            // Get user progress for lessons
            var userProgress = await _context.UserProgresses
                .Include(up => up.StepProgresses)
                .Where(up => up.UserId == userId)
                .ToListAsync();

            // Calculate statistics
            int totalModules = modules.Count;
            int totalLessons = modules.Sum(m => m.Lessons.Count);
            int completedLessons = userProgress.Count(up => up.IsCompleted);

            // Calculate module progress
            var moduleProgresses = new List<DTOs.Lessons.ModuleProgressDto>();
            int completedModules = 0;

            foreach (var module in modules)
            {
                var moduleLessons = module.Lessons.Count;
                var moduleCompletedLessons = userProgress
                    .Count(up => up.IsCompleted && module.Lessons.Any(l => l.Id == up.LessonId));

                bool isModuleCompleted = moduleLessons > 0 && moduleCompletedLessons == moduleLessons;
                if (isModuleCompleted)
                {
                    completedModules++;
                }

                int progressPercentage = moduleLessons > 0 ?
                    (int)Math.Round((moduleCompletedLessons / (double)moduleLessons) * 100) : 0;

                var lessonProgresses = new List<LessonProgressSummaryDto>();

                foreach (var lesson in module.Lessons.OrderBy(l => l.Order))
                {
                    var lessonProgress = userProgress
                        .FirstOrDefault(up => up.LessonId == lesson.Id);

                    // Calculate step completion
                    int stepCount = await _context.LessonSteps
                        .CountAsync(ls => ls.LessonId == lesson.Id);

                    int completedStepCount = lessonProgress?.StepProgresses
                        .Count(sp => sp.IsCompleted) ?? 0;

                    int lessonProgressPercentage = stepCount > 0 ?
                        (int)Math.Round((completedStepCount / (double)stepCount) * 100) : 0;

                    lessonProgresses.Add(new LessonProgressSummaryDto
                    {
                        LessonId = lesson.Id,
                        Title = lesson.Title,
                        Order = lesson.Order,
                        IsCompleted = lessonProgress?.IsCompleted ?? false,
                        ProgressPercentage = lessonProgressPercentage
                    });
                }

                moduleProgresses.Add(new DTOs.Lessons.ModuleProgressDto
                {
                    ModuleId = module.Id,
                    Title = module.Title,
                    Order = module.Order,
                    TotalLessons = moduleLessons,
                    CompletedLessons = moduleCompletedLessons,
                    ProgressPercentage = progressPercentage,
                    IsCompleted = isModuleCompleted,
                    LessonProgresses = lessonProgresses
                });
            }

            // Calculate overall progress percentage
            int overallProgressPercentage = totalLessons > 0 ?
                (int)Math.Round((completedLessons / (double)totalLessons) * 100) : 0;

            return new UserProgressResponse
            {
                TotalModules = totalModules,
                CompletedModules = completedModules,
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                OverallProgressPercentage = overallProgressPercentage,
                ModuleProgresses = moduleProgresses
            };
        }

        // Helper methods
        private async Task<bool> IsModuleCompletedAsync(Guid moduleId, Guid userId)
        {
            var module = await _context.Modules
                .Include(m => m.Lessons)
                .Include(m => m.Quizzes)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null)
                return false;

            // Check if all lessons are completed
            bool allLessonsCompleted = true;
            foreach (var lesson in module.Lessons)
            {
                var lessonCompleted = await _context.UserProgresses
                    .AnyAsync(up => up.UserId == userId && up.LessonId == lesson.Id && up.IsCompleted);

                if (!lessonCompleted)
                {
                    allLessonsCompleted = false;
                    break;
                }
            }

            // Check if all quizzes are attempted
            bool allQuizzesAttempted = true;
            foreach (var quiz in module.Quizzes)
            {
                var quizAttempted = await _context.QuizResults
                    .AnyAsync(qr => qr.UserId == userId && qr.QuizId == quiz.Id);

                if (!quizAttempted)
                {
                    allQuizzesAttempted = false;
                    break;
                }
            }

            return allLessonsCompleted && allQuizzesAttempted;
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