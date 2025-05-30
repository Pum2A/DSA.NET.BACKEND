using System;
using System.Threading.Tasks;
using DSA.DTOs.Auth;

namespace DSA.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest model);
        Task<AuthResponse> LoginAsync(LoginRequest model);
        Task<bool> LogoutAsync(string? refreshToken);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<UserDto?> GetUserByIdAsync(Guid userId);
        Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest model);
        Task<AuthResponse> ForgotPasswordAsync(string email);
        Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest model);
        Task<AuthResponse> VerifyEmailAsync(string token);
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class VerifyEmailRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}