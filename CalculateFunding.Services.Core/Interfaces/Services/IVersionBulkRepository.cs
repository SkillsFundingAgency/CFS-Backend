using System.Net;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Versioning;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Services
{
    public interface IVersionBulkRepository<T> : IHealthChecker where T : VersionedItem
    {
        Task<T> SaveVersion(T newVersion, string partitionKey);

        Task<T> CreateVersion(T newVersion,
            T currentVersion = null,
            string partitionKey = null,
            bool incrementFromCurrentVersion = false);

        Task<HttpStatusCode> SaveVersion(T newVersion);
    }
}
