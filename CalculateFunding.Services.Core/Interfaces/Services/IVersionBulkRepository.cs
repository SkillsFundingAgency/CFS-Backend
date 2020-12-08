using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Versioning;
using Polly;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Services
{
    public interface IVersionBulkRepository<T> : IHealthChecker where T : VersionedItem
    {
        Task<T> SaveVersion(T newVersion, string partitionKey);
    }
}
