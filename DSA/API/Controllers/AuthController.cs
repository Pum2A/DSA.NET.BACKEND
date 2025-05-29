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

        [Authorize] // Wymaga ważnego ciasteczka 'jwt' do wywołania
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Prostszy sposób pobrania ID

            // Chociaż [Authorize] to sprawdza, dodatkowa weryfikacja nie zaszkodzi
            if (string.IsNullOrEmpty(userId))
            {
                // _logger?.LogWarning("Logout attempt without valid user identifier in claims.");
                // Teoretycznie nie powinno się zdarzyć przez [Authorize]
                return Unauthorized("Invalid session.");
            }

            // Opcjonalnie: Wywołaj logikę biznesową (np. unieważnienie refresh tokena w bazie)
            // To jest dobra praktyka, ale nie wpływa bezpośrednio na usunięcie cookie 'jwt'
            var serviceResult = await _authService.LogoutAsync(userId);
            if (!serviceResult)
            {
                // _logger?.LogWarning($"Logout service failed for user {userId}. Proceeding to clear cookies anyway.");
                // Można zalogować błąd, ale kontynuować usuwanie ciasteczek
            }


            // --- KLUCZOWA POPRAWKA ---
            // Utwórz obiekt CookieOptions z Domain i Path pasującymi do SetTokenCookies
            var cookieOptions = new CookieOptions
            {
                // HttpOnly i Secure nie są potrzebne przy usuwaniu, ale Domain i Path są KLUCZOWE
                Domain = "dsadotnet-481e228cd0ec.herokuapp.com", // MUSI pasować do tego z SetTokenCookies
                Path = "/", // MUSI pasować do tego z SetTokenCookies
                // SameSite i Secure nie są technicznie wymagane przez specyfikację do usunięcia,
                // ale dodanie ich nie zaszkodzi i może być wymagane przez niektóre implementacje.
                Secure = true,
                SameSite = SameSiteMode.None
            };

            // Usuń ciasteczka używając tych samych opcji Domain i Path
            Response.Cookies.Delete("jwt", cookieOptions);
            Response.Cookies.Delete("refreshToken", cookieOptions);
            // --- KONIEC POPRAWKI ---

            // _logger?.LogInformation($"User {userId} logged out successfully.");
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
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddMinutes(60),
                Secure = true, // MUSI BYĆ true dla HTTPS
                SameSite = SameSiteMode.None, // WYMAGANE dla cross-origin
                Path = "/",
                Domain = "dsadotnet-481e228cd0ec.herokuapp.com" // Dodaj swój domain Heroku
            };

            Response.Cookies.Append("jwt", token, cookieOptions);

            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Domain = "dsadotnet-481e228cd0ec.herokuapp.com"
            };

            Response.Cookies.Append("refreshToken", refreshToken, refreshTokenOptions);
        }
    }
}