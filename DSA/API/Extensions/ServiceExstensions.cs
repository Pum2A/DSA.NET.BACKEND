using DSA.Core.Entities;
using DSA.Core.Interfaces;
using DSA.Infrastructure;
using DSA.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace DSA.API.Extensions
{
    public static class ServiceExtensions
    {
        public static void AddAuthServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;

                // User settings
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // JWT Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JWT:ValidIssuer"],
                    ValidAudience = configuration["JWT:ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"])),
                    ClockSkew = TimeSpan.Zero
                };

                // Dodaj obsługę HTTP cookies
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Próbuj pobrać token z ciasteczka, jeśli nie ma go w nagłówku
                        if (string.IsNullOrEmpty(context.Token) && context.Request.Cookies.ContainsKey("jwt"))
                        {
                            context.Token = context.Request.Cookies["jwt"];
                        }
                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                };
            });

            // Custom services
            services.AddScoped<IAuthService, AuthService>();
        }
    }
}