using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DSA.Data;
using DSA.DTOs.Auth;
using DSA.Helpers;
using DSA.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DSA.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest model)
        {
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return new AuthResponse { Success = false, Message = "User with this email already exists." };

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                return new AuthResponse { Success = false, Message = "Username is already taken." };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = model.Email,
                Username = model.Username,
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                XpPoints = 0,
                CurrentStreak = 0,
                MaxStreak = 0
            };

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            _context.Users.Add(user);

            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refreshTokenEntity);

            await _context.SaveChangesAsync();

            var verificationToken = GenerateRandomToken();

            // TODO: Save verification token in database

            await _emailService.SendEmailVerificationAsync(user.Email, user.Username, verificationToken);

            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful. Please verify your email.",
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = MapToUserDto(user)
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest model)
        {
            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
                return new AuthResponse { Success = false, Message = "Invalid email or password." };

            var now = DateTime.UtcNow;

            var tokensToDelete = await _context.RefreshTokens
                .Where(rt =>
                    rt.UserId == user.Id &&
                    (rt.RevokedAt != null || rt.Expires <= now || rt.CreatedAt.AddDays(7) <= now)
                ).ToListAsync();

            if (tokensToDelete.Any())
                _context.RefreshTokens.RemoveRange(tokensToDelete);

            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = GenerateRefreshToken(),
                Expires = now.AddDays(7),
                CreatedAt = now
            };
            _context.RefreshTokens.Add(refreshTokenEntity);

            user.LastActivityDate = now;
            await UpdateUserStreakAsync(user);

            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful.",
                Token = token,
                RefreshToken = refreshTokenEntity.Token,
                ExpiresAt = now.AddHours(1),
                User = MapToUserDto(user)
            };
        }

        public async Task<bool> LogoutAsync(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .SingleOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
                return false;

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReasonRevoked = "Logout";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .SingleOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null || !storedToken.IsActive)
                return new AuthResponse { Success = false, Message = "Invalid or expired refresh token." };

            var user = storedToken.User;

            var newRefreshToken = GenerateRefreshToken();
            storedToken.ReplacedByToken = newRefreshToken;
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReasonRevoked = "Refresh token rotation";

            user.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            });

            RemoveOldRefreshTokens(user);

            var token = GenerateJwtToken(user);

            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "Token refreshed successfully.",
                Token = token,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = MapToUserDto(user)
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user == null ? null : MapToUserDto(user);
        }

        public async Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest model)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found." };

            if (!PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                return new AuthResponse { Success = false, Message = "Current password is incorrect." };

            if (model.NewPassword != model.ConfirmNewPassword)
                return new AuthResponse { Success = false, Message = "New passwords don't match." };

            user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AuthResponse { Success = true, Message = "Password changed successfully." };
        }

        public async Task<AuthResponse> ForgotPasswordAsync(string email)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return new AuthResponse { Success = true, Message = "If your email is registered, you will receive a password reset link." };

            var resetToken = GenerateRandomToken();

            // TODO: Save reset token in database

            await _emailService.SendPasswordResetAsync(user.Email, user.Username, resetToken);

            return new AuthResponse
            {
                Success = true,
                Message = "If your email is registered, you will receive a password reset link."
            };
        }

        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest model)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
                return new AuthResponse { Success = false, Message = "Invalid token or email." };

            user.PasswordHash = PasswordHelper.HashPassword(model.Password);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AuthResponse { Success = true, Message = "Password has been reset successfully." };
        }

        public async Task<AuthResponse> VerifyEmailAsync(string token)
        {
            // TODO: Validate token from database

            var user = await _context.Users.FindAsync(Guid.Empty); // Replace with actual lookup

            if (user == null)
                return new AuthResponse { Success = false, Message = "Invalid verification token." };

            user.EmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AuthResponse { Success = true, Message = "Email verified successfully." };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSecret = _configuration["Jwt:Secret"];
            if (string.IsNullOrEmpty(jwtSecret))
                throw new InvalidOperationException("JWT key is not configured");

            var key = Encoding.UTF8.GetBytes(jwtSecret);

            var claims = new List<Claim>
            {
                new Claim("id", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:ValidIssuer"],
                Audience = _configuration["Jwt:ValidAudience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private string GenerateRandomToken()
        {
            return Guid.NewGuid().ToString();
        }

        private void RemoveOldRefreshTokens(User user)
        {
            var now = DateTime.UtcNow;
            var oldTokens = _context.RefreshTokens
                .Where(rt =>
                    rt.UserId == user.Id &&
                    (rt.RevokedAt != null || rt.Expires <= now || rt.CreatedAt.AddDays(7) <= now)
                ).ToList();

            if (oldTokens.Any())
                _context.RefreshTokens.RemoveRange(oldTokens);
        }

        private async Task UpdateUserStreakAsync(User user)
        {
            if (user.LastActivityDate.HasValue)
            {
                var lastActivity = user.LastActivityDate.Value.Date;
                var today = DateTime.UtcNow.Date;
                var yesterday = today.AddDays(-1);

                if (lastActivity == today)
                {
                    return;
                }
                else if (lastActivity == yesterday)
                {
                    user.CurrentStreak++;
                    if (user.CurrentStreak > user.MaxStreak)
                    {
                        user.MaxStreak = user.CurrentStreak;
                    }
                }
                else if (lastActivity < yesterday)
                {
                    user.CurrentStreak = 1;
                }
            }
            else
            {
                user.CurrentStreak = 1;
                user.MaxStreak = 1;
            }
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Avatar = user.Avatar,
                EmailVerified = user.EmailVerified,
                XpPoints = user.XpPoints,
                CurrentStreak = user.CurrentStreak,
                MaxStreak = user.MaxStreak
            };
        }
    }
}