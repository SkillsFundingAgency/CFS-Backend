using Microsoft.Azure.ServiceBus;
using CalculateFunding.Common.Models.HealthCheck;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers
{
    public interface IProviderSnapshotDataLoadService : IHealthChecker
    {
        Task LoadProviderSnapshotData(Message message);
    }
}
