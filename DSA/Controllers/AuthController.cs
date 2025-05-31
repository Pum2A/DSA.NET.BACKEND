using DSA.DTOs.Auth;
using DSA.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using DSA.Data;

namespace DSA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        private const string AccessTokenCookieName = "dsa-access-token";
        private const string RefreshTokenCookieName = "dsa-refresh-token";

        public AuthController(IAuthService authService, IConfiguration configuration, ApplicationDbContext context)
        {
            _authService = authService;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(model);

            if (!result.Success)
                return BadRequest(result);

            SetAuthCookies(result.Token, result.RefreshToken, result.ExpiresAt);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(model);

            if (!result.Success)
                return BadRequest(result);

            SetAuthCookies(result.Token, result.RefreshToken, result.ExpiresAt);

            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            var refreshToken = Request.Cookies[RefreshTokenCookieName];
            await _authService.LogoutAsync(refreshToken);

            ClearAuthCookies();

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> RefreshToken()
        {
            var refreshToken = Request.Cookies[RefreshTokenCookieName];

            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest(new { message = "Refresh token is required" });

            var result = await _authService.RefreshTokenAsync(refreshToken);

            if (!result.Success)
            {
                ClearAuthCookies();
                return BadRequest(result);
            }

            SetAuthCookies(result.Token, result.RefreshToken, result.ExpiresAt);

            return Ok(result);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var userDto = await _authService.GetUserByIdAsync(Guid.Parse(userId));

            if (userDto == null)
                return NotFound();

            return Ok(userDto);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword(ChangePasswordRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _authService.ChangePasswordAsync(Guid.Parse(userId), model);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ForgotPasswordAsync(model.Email);

            return Ok(new { message = "If your email is registered, you will receive a password reset link." });
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword(ResetPasswordRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ResetPasswordAsync(model);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("verify-email")]
        public async Task<ActionResult> VerifyEmail(VerifyEmailRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.VerifyEmailAsync(model.Token);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("id")?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        return Ok(new
                        {
                            isAuthenticated = true,
                            user = new
                            {
                                user.Id,
                                user.Email,
                                user.Username,
                                user.Avatar,
                                user.EmailVerified,
                                user.XpPoints,
                                user.CurrentStreak,
                                user.MaxStreak
                            }
                        });
                    }
                }
            }

            var refreshToken = Request.Cookies[RefreshTokenCookieName];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var storedToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .SingleOrDefaultAsync(rt => rt.Token == refreshToken && rt.RevokedAt == null && rt.Expires > DateTime.UtcNow);

                if (storedToken != null && storedToken.User != null)
                {
                    var token = GenerateJwtToken(storedToken.User);
                    SetAuthCookies(token, refreshToken, DateTime.UtcNow.AddHours(1));
                    return Ok(new
                    {
                        isAuthenticated = true,
                        user = new
                        {
                            storedToken.User.Id,
                            storedToken.User.Email,
                            storedToken.User.Username,
                            storedToken.User.Avatar,
                            storedToken.User.EmailVerified,
                            storedToken.User.XpPoints,
                            storedToken.User.CurrentStreak,
                            storedToken.User.MaxStreak
                        }
                    });
                }
            }

            ClearAuthCookies();
            return Ok(new { isAuthenticated = false });
        }

        private void SetAuthCookies(string? accessToken, string? refreshToken, DateTime? expiresAt)
        {
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken) || !expiresAt.HasValue)
                return;

            var accessCookie = new CookieOptions
            {
                HttpOnly = true,
                Expires = expiresAt.Value,
                SameSite = SameSiteMode.Strict,
                Secure = true
            };
            Response.Cookies.Append(AccessTokenCookieName, accessToken, accessCookie);

            var refreshCookie = new CookieOptions
            {
                HttpOnly = true,
                Expires = expiresAt.Value.AddDays(7),
                SameSite = SameSiteMode.Strict,
                Secure = true,
                Path = "/api/Auth"
            };
            Response.Cookies.Append(RefreshTokenCookieName, refreshToken, refreshCookie);
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = true
            });
            Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
            {
                Path = "/api/Auth",
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = true
            });
        }

        private string GenerateJwtToken(DSA.Models.User user)
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
    }
}