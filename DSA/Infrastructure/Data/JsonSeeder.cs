using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DSA.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DSA.Infrastructure.Data
{
    public static class JsonSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

                logger.LogInformation("Rozpoczęcie seedowania danych z plików JSON...");

                // Sprawdź czy baza danych jest pusta
                if (await context.Modules.AnyAsync())
                {
                    logger.LogInformation("Baza danych zawiera już dane. Seedowanie pominięte.");
                    return;
                }

                // Odczytaj i zapisz moduły
                var modules = await ReadFromJsonFileAsync<Module>("SeedData/modules.json");
                if (modules != null && modules.Count > 0)
                {
                    logger.LogInformation($"Dodawanie {modules.Count} modułów...");
                    context.Modules.AddRange(modules);
                    await context.SaveChangesAsync();
                }

                // Odczytaj i zapisz lekcje
                var lessons = await ReadFromJsonFileAsync<Lesson>("SeedData/lessons.json");
                if (lessons != null && lessons.Count > 0)
                {
                    logger.LogInformation($"Dodawanie {lessons.Count} lekcji...");
                    context.Lessons.AddRange(lessons);
                    await context.SaveChangesAsync();
                }

                // Odczytaj i zapisz kroki
                var steps = await ReadFromJsonFileAsync<Step>("SeedData/steps.json");
                if (steps != null && steps.Count > 0)
                {
                    logger.LogInformation($"Dodawanie {steps.Count} kroków...");
                    context.Steps.AddRange(steps);
                    await context.SaveChangesAsync();
                }

                logger.LogInformation("Seedowanie zakończone pomyślnie.");
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
                logger.LogError(ex, "Wystąpił błąd podczas seedowania danych");
                throw;
            }
        }

        private static async Task<List<T>> ReadFromJsonFileAsync<T>(string filePath) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Plik {filePath} nie istnieje.");
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<T>>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd odczytu pliku {filePath}: {ex.Message}");
                return null;
            }
        }
    }
}