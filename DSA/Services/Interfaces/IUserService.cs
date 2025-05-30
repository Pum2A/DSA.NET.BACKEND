using System;
using System.Threading.Tasks;
using DSA.DTOs.Users;

namespace DSA.Services
{
    public interface IUserService
    {
        Task<UserProfileDto?> GetUserProfileAsync(Guid userId);
        Task<UpdateProfileResult> UpdateProfileAsync(Guid userId, UpdateProfileRequest model);
        Task<PublicUserProfileDto?> GetPublicUserProfileAsync(Guid userId);
        Task<UserRankingResponse> GetUserRankingAsync(UserRankingRequest request);
        Task<UserActivityResponse> GetUserActivityAsync(Guid userId, UserActivityRequest request);
        Task<UserProgressResponse> GetUserGlobalProgressAsync(Guid userId);
        Task<UserXpResponse> GetUserXpAsync(Guid userId);
        Task<UserStreakResponse> GetUserStreakAsync(Guid userId);
    }
}