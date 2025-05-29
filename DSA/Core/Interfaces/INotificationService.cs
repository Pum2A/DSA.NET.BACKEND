using System.Threading.Tasks;

namespace DSA.Core.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string message, string type = "info");
        Task MarkNotificationAsReadAsync(int notificationId);
    }
}