using DSA.Core.DTOs.Auth;
using System.Threading.Tasks;

namespace DSA.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterUserAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken);
        Task<bool> LogoutAsync(string userId);
        Task<UserProfileDto> GetUserByIdAsync(string userId);
    }
}