using System.Threading.Tasks;

namespace DSA.Services
{
    public interface IEmailService
    {
        Task SendEmailVerificationAsync(string email, string username, string token);
        Task SendPasswordResetAsync(string email, string username, string token);
    }
}