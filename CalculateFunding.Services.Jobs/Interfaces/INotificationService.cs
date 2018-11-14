using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface INotificationService
    {
        Task SendNotification(JobNotification jobNotification);
    }
}
