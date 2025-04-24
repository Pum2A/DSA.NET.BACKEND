using DSA.Core.DTOs.Lessons;
using DSA.Core.Entities;
using DSA.Core.Extensions;
using DSA.Core.Interfaces;
using DSA.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DSA.Infrastructure.Services
{
    public class LessonService : ILessonService
    {
        private readonly ILessonRepository _lessonRepository;
        private readonly IModuleRepository _moduleRepository;
        private readonly IUserProgressRepository _userProgressRepository;
        private readonly ApplicationDbContext _context;

        public LessonService(
            ILessonRepository lessonRepository,
            IModuleRepository moduleRepository,
            IUserProgressRepository userProgressRepository,
            ApplicationDbContext context)
        {
            _lessonRepository = lessonRepository;
            _moduleRepository = moduleRepository;
            _userProgressRepository = userProgressRepository;
            _context = context;
        }

        public async Task<IEnumerable<ModuleDto>> GetAllModulesAsync()
        {
            var modules = await _moduleRepository.GetAllAsync();

            // Pobierz wszystkie lekcje dla tych modułów
            var modulesList = modules.ToList();
            foreach (var module in modulesList)
            {
                var moduleWithLessons = await _moduleRepository.GetModuleWithLessonsAsync(module.Id);
                module.Lessons = moduleWithLessons.Lessons;
            }

            return modulesList.ToDto();
        }

        public async Task<ModuleDto> GetModuleByIdAsync(string moduleId)
        {
            var module = await _moduleRepository.GetModuleWithLessonsByExternalIdAsync(moduleId);
            return module?.ToDto();
        }

        public async Task<LessonDto> GetLessonByIdAsync(string lessonId)
        {
            var lesson = await _lessonRepository.GetLessonWithStepsByExternalIdAsync(lessonId);
            return lesson?.ToDto();
        }

        public async Task<UserProgressDto> GetLessonProgressAsync(string userId, string lessonId)
        {
            var lesson = await _lessonRepository.GetByExternalIdAsync(lessonId);
            if (lesson == null)
                return null;

            var progress = await _userProgressRepository.GetUserProgressAsync(userId, lesson.Id);
            if (progress == null)
                return new UserProgressDto
                {
                    UserId = userId,
                    LessonId = lesson.Id,
                    IsCompleted = false,
                    CurrentStepIndex = 0,
                    StartedAt = DateTime.UtcNow,
                    XpEarned = 0
                };

            return progress.ToDto();
        }

        public async Task<ModuleProgressDto> GetModuleProgressAsync(string userId, string moduleId)
        {
            var module = await _moduleRepository.GetModuleWithLessonsByExternalIdAsync(moduleId);
            if (module == null)
                return null;

            var allProgressRecords = await _userProgressRepository.GetUserProgressForModuleAsync(userId, module.Id);

            var result = new ModuleProgressDto
            {
                TotalLessons = module.Lessons.Count,
                CompletedLessons = allProgressRecords.Count(p => p.IsCompleted),
                InProgressLessons = allProgressRecords.Count(p => !p.IsCompleted && p.StartedAt != null)
            };

            return result;
        }

        public async Task<bool> CompleteStepAsync(string userId, string lessonId, int stepIndex)
        {
            var lesson = await _lessonRepository.GetLessonWithStepsByExternalIdAsync(lessonId);
            if (lesson == null)
                return false;

            // Sprawdź czy istnieje już progress dla tej lekcji
            var progress = await _userProgressRepository.GetUserProgressAsync(userId, lesson.Id);

            if (progress == null)
            {
                // Stwórz nowy progress
                progress = new UserProgress
                {
                    UserId = userId,
                    LessonId = lesson.Id,
                    IsCompleted = false,
                    CurrentStepIndex = stepIndex + 1, // Następny krok
                    StartedAt = DateTime.UtcNow,
                    XpEarned = 0
                };

                await _userProgressRepository.AddAsync(progress);
            }
            else
            {
                // Zaktualizuj istniejący progress
                progress.CurrentStepIndex = stepIndex + 1;
                await _userProgressRepository.UpdateAsync(progress);
            }

            return true;
        }

        public async Task<bool> CompleteLessonAsync(string userId, string lessonId)
        {
            var lesson = await _lessonRepository.GetByExternalIdAsync(lessonId);
            if (lesson == null)
                return false;

            var progress = await _userProgressRepository.GetUserProgressAsync(userId, lesson.Id);
            bool isFirstCompletion = false;

            if (progress == null)
            {
                // Stwórz nowy progress jako ukończony
                progress = new UserProgress
                {
                    UserId = userId,
                    LessonId = lesson.Id,
                    IsCompleted = true,
                    CurrentStepIndex = 0,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    XpEarned = lesson.XpReward
                };

                await _userProgressRepository.AddAsync(progress);
                isFirstCompletion = true;
            }
            else if (!progress.IsCompleted)
            {
                // Zaktualizuj istniejący progress jako ukończony, ale tylko jeśli nie był wcześniej ukończony
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
                progress.LastUpdated = DateTime.UtcNow;
                progress.XpEarned = lesson.XpReward;
                await _userProgressRepository.UpdateAsync(progress);
                isFirstCompletion = true;
            }
            else
            {
                // Lekcja już była ukończona, zaktualizuj tylko LastUpdated
                // Nie przyznawaj ponownie XP!
                progress.LastUpdated = DateTime.UtcNow;
                await _userProgressRepository.UpdateAsync(progress);
            }

            // Zaktualizuj punkty XP użytkownika tylko przy pierwszym ukończeniu
            if (isFirstCompletion)
            {
                await UpdateUserXpAsync(userId, lesson.XpReward);
            }

            return true;
        }

        private async Task UpdateUserXpAsync(string userId, int xpToAdd)
        {
            // Ta metoda aktualizuje punkty XP użytkownika
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.ExperiencePoints += xpToAdd;

                // Oblicz nowy poziom
                var baseXp = 100; // Bazowa ilość XP potrzebna do poziomu 2
                var multiplier = 1.5; // Mnożnik dla każdego kolejnego poziomu

                var level = 1;
                var requiredXp = baseXp;

                while (user.ExperiencePoints >= requiredXp)
                {
                    level++;
                    requiredXp = (int)(baseXp * Math.Pow(multiplier, level - 1));
                }

                user.Level = level;

                await _context.SaveChangesAsync();
            }
        }
    }
}