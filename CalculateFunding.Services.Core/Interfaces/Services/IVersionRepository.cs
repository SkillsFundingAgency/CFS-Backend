using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Versioning;

namespace CalculateFunding.Services.Core.Interfaces
{
    public interface IVersionRepository<T> : IHealthChecker where T : VersionedItem
    {
        public Task<T> CreateVersion(T newVersion, T currentVersion = null, string partitionKey = null, bool incrementFromCurrentVersion = false);

        public Task<HttpStatusCode> SaveVersion(T newVersion);

        public Task SaveVersion(T newVersion, string partitionKey);

        public Task<IEnumerable<T>> GetVersions(string entityId, string partitionKeyId = null);

        public Task<T> GetVersion(string entityId, int version);

        public Task SaveVersions(IEnumerable<T> newVersions, int maxDegreesOfParallelism = 30);

        public Task SaveVersions(IEnumerable<KeyValuePair<string, T>> newVersions, int maxDegreesOfParallelism = 30);
        public Task DeleteVersions(IEnumerable<KeyValuePair<string, T>> newVersions, int maxDegreesOfParallelism = 30);

        public Task<int> GetNextVersionNumber(T version = null, int currentVersion = 0, string partitionKeyId = null, bool incrementFromCurrentVersion = false);
    }
}
