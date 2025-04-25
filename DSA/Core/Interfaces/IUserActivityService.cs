using DSA.Core.Entities;
using System.Threading.Tasks;

namespace DSA.Core.Interfaces
{
    public interface IUserActivityService
    {
        Task LogActivityAsync(string userId, UserActionType actionType, string? referenceId = null, string? additionalInfo = null);
    }
}