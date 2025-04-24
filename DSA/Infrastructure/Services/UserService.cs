using DSA.Core.Entities;
using DSA.Core.Interfaces;
using DSA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DSA.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;


        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;


        }



        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<bool> AddExperienceAsync(string userId, int amount)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User {userId} not found when trying to add XP");
                    return false;
                }

                // Zapisz poprzedni poziom
                int previousLevel = user.Level;

                // Dodaj doświadczenie
                user.ExperiencePoints += amount;
                
                // Zaktualizuj poziom
                user.Level = CalculateLevel(user.ExperiencePoints);
                
                // Zapisz zmiany
                await _context.SaveChangesAsync();
                
                // Sprawdź czy nastąpił awans na wyższy poziom
                if (user.Level > previousLevel)
                {
                    _logger.LogInformation($"User {userId} leveled up from {previousLevel} to {user.Level}");
                    // Tutaj można dodać kod do obsługi awansu (np. wysłanie powiadomienia)
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding XP for user {userId}");
                return false;
            }
        }

        public async Task<int> GetUserLevelAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Level ?? 1;
        }



        // Prosta formuła poziomów na podstawie XP
        private int CalculateLevel(int xp)
        {
            // Przykładowa formuła: każdy poziom wymaga o 100 więcej XP niż poprzedni
            // Poziom 1: 0-99 XP
            // Poziom 2: 100-299 XP
            // Poziom 3: 300-599 XP
            // itd.
            
            if (xp < 100) return 1;
            
            int level = 1;
            int requiredXp = 100;
            int totalRequiredXp = 100;
            
            while (xp >= totalRequiredXp)
            {
                level++;
                requiredXp += 100;
                totalRequiredXp += requiredXp;
            }
            
            return level;
        }

    }
}