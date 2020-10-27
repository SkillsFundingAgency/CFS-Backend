using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderSnapshotDataLoadService : IJobProcessingService,  IHealthChecker
    {
    }
}
