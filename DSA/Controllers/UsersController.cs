using System;
using System.Threading.Tasks;
using DSA.DTOs.Users;
using DSA.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile()
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var profile = await _userService.GetUserProfileAsync(Guid.Parse(userId));

            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        [HttpPatch("me")]
        public async Task<ActionResult<UserProfileDto>> UpdateMyProfile(UpdateProfileRequest model)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _userService.UpdateProfileAsync(Guid.Parse(userId), model);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<PublicUserProfileDto>> GetUserProfile(Guid userId)
        {
            var profile = await _userService.GetPublicUserProfileAsync(userId);

            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        [HttpGet("ranking")]
        public async Task<ActionResult<UserRankingResponse>> GetUserRanking([FromQuery] UserRankingRequest request)
        {
            var ranking = await _userService.GetUserRankingAsync(request);
            return Ok(ranking);
        }

        [HttpGet("me/activity")]
        public async Task<ActionResult<UserActivityResponse>> GetMyActivity([FromQuery] UserActivityRequest request)
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var activity = await _userService.GetUserActivityAsync(Guid.Parse(userId), request);
            return Ok(activity);
        }

        [HttpGet("me/progress")]
        public async Task<ActionResult<UserProgressResponse>> GetMyProgress()
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var progress = await _userService.GetUserGlobalProgressAsync(Guid.Parse(userId));
            return Ok(progress);
        }

        [HttpGet("me/xp")]
        public async Task<ActionResult<UserXpResponse>> GetMyXp()
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var xp = await _userService.GetUserXpAsync(Guid.Parse(userId));
            return Ok(xp);
        }

        [HttpGet("me/streak")]
        public async Task<ActionResult<UserStreakResponse>> GetMyStreak()
        {
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var streak = await _userService.GetUserStreakAsync(Guid.Parse(userId));
            return Ok(streak);
        }
    }
}