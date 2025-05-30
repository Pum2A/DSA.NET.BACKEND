using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DSA.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendEmailVerificationAsync(string email, string username, string token)
        {
            // In a real application, you would implement email sending here
            // using services like SendGrid, SMTP, etc.

            // For development purposes, we're just logging the info
            Console.WriteLine($"[Email Service] Sending verification email to {email} ({username})");
            Console.WriteLine($"[Email Service] Verification token: {token}");
            Console.WriteLine($"[Email Service] Verification URL: {_configuration["AppUrl"]}/verify-email?token={token}");

            // In production, you'd return the actual sending task
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string username, string token)
        {
            // In a real application, you would implement email sending here
            Console.WriteLine($"[Email Service] Sending password reset email to {email} ({username})");
            Console.WriteLine($"[Email Service] Reset token: {token}");
            Console.WriteLine($"[Email Service] Reset URL: {_configuration["AppUrl"]}/reset-password?token={token}&email={email}");

            return Task.CompletedTask;
        }
    }
}