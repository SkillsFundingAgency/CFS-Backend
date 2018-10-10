using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Versioning;

namespace CalculateFunding.Services.Core.Interfaces
{
    public interface IVersionRepository<T> where T : VersionedItem
    {
        Task<T> CreateVersion(T newVersion, T currentVersion = null, string partitionKey = null);

        Task SaveVersion(T newVersion);

        Task<IEnumerable<T>> GetVersions(string entityId, string partitionKeyId = null);

        Task<T> GetVersion(string entityId, int version);

        Task SaveVersions(IEnumerable<T> newVersions, int maxDegreesOfParallelism = 30);

        Task SaveVersions(IEnumerable<KeyValuePair<string, T>> newVersions, int maxDegreesOfParallelism = 30);

        Task<int> GetNextVersionNumber(T version, string partitionKeyId = null);
    }
}
