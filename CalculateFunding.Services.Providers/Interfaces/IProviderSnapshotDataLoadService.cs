using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderSnapshotDataLoadService : IHealthChecker
    {
        Task LoadProviderSnapshotData(Message message);
    }
}
