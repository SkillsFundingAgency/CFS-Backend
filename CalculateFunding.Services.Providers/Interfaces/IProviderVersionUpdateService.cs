using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Services.Processing.Interfaces;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderVersionUpdateService : IJobProcessingService, IHealthChecker
    {
    }
}
