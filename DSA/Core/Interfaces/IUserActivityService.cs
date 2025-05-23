// DSA.Core/Interfaces/IUserActivityService.cs
using DSA.Core.Entities;
using DSA.Core.Entities.User;
using DSA.Core.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DSA.Core.Interfaces
{
    public interface IUserActivityService
    {
        Task LogActivityAsync(string userId, UserActionType actionType, string referenceId = null, string additionalInfo = null);
        Task<List<UserActivity>> GetUserActivitiesAsync(string userId, int count = 10);
    }
}