using DSA.Core.DTOs.Auth;
using DSA.Core.Entities;
using DSA.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DSA.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterUserAsync(RegisterRequest request)
        {
            var userExists = await _userManager.FindByEmailAsync(request.Email);
            if (userExists != null)
                return new AuthResponse { Succeeded = false, Errors = new[] { "User with this email already exists" } };

            var userNameExists = await _userManager.FindByNameAsync(request.UserName);
            if (userNameExists != null)
                return new AuthResponse { Succeeded = false, Errors = new[] { "Username is already taken" } };

            ApplicationUser user = new ApplicationUser()
            {
                Email = request.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = request.UserName,
                FirstName = request.FirstName,
                LastName = request.LastName,
                ExperiencePoints = 0,
                Level = 1
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return new AuthResponse { Succeeded = false, Errors = result.Errors.Select(e => e.Description).ToArray() };

            // Domyślnie przypisz rolę "User"
            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole("User"));

            await _userManager.AddToRoleAsync(user, "User");

            // Generuj token JWT
            var token = await GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Zapisz refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            // Pobierz role dla tokena
            var userRoles = await _userManager.GetRolesAsync(user);

            return new AuthResponse
            {
                Succeeded = true,
                Token = token,
                RefreshToken = refreshToken,
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = userRoles.ToArray(),
                Expiration = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo.ToString("yyyy-MM-dd HH:mm:ss"),
                JoinedAt = DateTime.UtcNow
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return new AuthResponse { Succeeded = false, Errors = new[] { "Invalid email or password" } };

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
                return new AuthResponse { Succeeded = false, Errors = new[] { "Invalid email or password" } };

            // Generuj token JWT
            var token = await GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Zapisz refresh token
            int refreshTokenValidityInDays = request.RememberMe ? 30 : 7;
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenValidityInDays);
            await _userManager.UpdateAsync(user);

            // Pobierz role dla tokena
            var userRoles = await _userManager.GetRolesAsync(user);

            return new AuthResponse
            {
                Succeeded = true,
                Token = token,
                RefreshToken = refreshToken,
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = userRoles.ToArray(),
                Expiration = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo.ToString("yyyy-MM-dd HH:mm:ss"),
                JoinedAt = DateTime.UtcNow
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken)
        {
            // Walidacja tokena JWT
            var principal = GetPrincipalFromExpiredToken(token);
            if (principal == null)
                return new AuthResponse { Succeeded = false, Errors = new[] { "Invalid token" } };

            // Pobierz userId z tokena
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return new AuthResponse { Succeeded = false, Errors = new[] { "Invalid token" } };

            // Znajdź użytkownika
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return new AuthResponse { Succeeded = false, Errors = new[] { "Invalid or expired refresh token" } };

            // Generuj nowy token JWT
            var newToken = await GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Zapisz nowy refresh token
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            // Pobierz role dla tokena
            var userRoles = await _userManager.GetRolesAsync(user);

            return new AuthResponse
            {
                Succeeded = true,
                Token = newToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = userRoles.ToArray(),
                Expiration = new JwtSecurityTokenHandler().ReadJwtToken(newToken).ValidTo.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        public async Task<bool> LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            // Anuluj refresh token
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }

        public async Task<UserProfileDto> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ExperiencePoints = user.ExperiencePoints,
                Level = user.Level,
                Roles = roles.ToArray()
            };
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var tokenValidityInMinutes = int.Parse(_configuration["JWT:TokenValidityInMinutes"]);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddMinutes(tokenValidityInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"])),
                ValidIssuer = _configuration["JWT:ValidIssuer"],
                ValidAudience = _configuration["JWT:ValidAudience"],
                ValidateLifetime = false // Nie sprawdzaj czasu ważności tokena
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            
            if (!(securityToken is JwtSecurityToken jwtSecurityToken) || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
    }
}