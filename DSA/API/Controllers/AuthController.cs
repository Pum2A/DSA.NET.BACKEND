using DSA.Core.DTOs.Auth;
using DSA.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DSA.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterUserAsync(request);

            if (!result.Succeeded)
                return BadRequest(result);

            // Ustawienie ciasteczek HttpOnly
            SetTokenCookies(result.Token, result.RefreshToken);

            // Zwracamy dane użytkownika bez tokenów
            return Ok(new
            {
                succeeded = result.Succeeded,
                userId = result.UserId,
                userName = result.UserName,
                email = result.Email,
                roles = result.Roles,
                errors = result.Errors
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);

            if (!result.Succeeded)
                return BadRequest(result);

            // Ustawienie ciasteczek HttpOnly
            SetTokenCookies(result.Token, result.RefreshToken);

            // Zwracamy dane użytkownika bez tokenów
            return Ok(new
            {
                succeeded = result.Succeeded,
                userId = result.UserId,
                userName = result.UserName,
                email = result.Email,
                roles = result.Roles,
                errors = result.Errors
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            // Pobierz tokeny z ciasteczek
            var token = Request.Cookies["jwt"];
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
                return BadRequest(new { succeeded = false, errors = new[] { "Invalid token or refresh token" } });

            var result = await _authService.RefreshTokenAsync(token, refreshToken);

            if (!result.Succeeded)
                return BadRequest(result);

            // Zaktualizuj ciasteczka
            SetTokenCookies(result.Token, result.RefreshToken);

            // Zwracamy tylko informację o sukcesie
            return Ok(new { succeeded = true });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return BadRequest("Invalid user");

            var result = await _authService.LogoutAsync(userId);

            if (!result)
                return BadRequest("Failed to logout");

            // Usuń ciasteczka
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refreshToken");

            return Ok(new { message = "You have been successfully logged out" });
        }

        [Authorize]
        [HttpGet("user")]
        public async Task<IActionResult> GetUserInfo()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return BadRequest("Invalid user");

            var user = await _authService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { succeeded = false, errors = new[] { "User not found" } });

            return Ok(user);
        }

        private void SetTokenCookies(string token, string refreshToken)
        {
            // Konfiguracja ciasteczka JWT
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddMinutes(60),
                Secure = false, // Wymagaj HTTPS
                SameSite = SameSiteMode.Strict,
                Path = "/"
            };
            Response.Cookies.Append("jwt", token, cookieOptions);

            // Konfiguracja ciasteczka Refresh Token
            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            };
            Response.Cookies.Append("refreshToken", refreshToken, refreshTokenOptions);
        }
    }
}