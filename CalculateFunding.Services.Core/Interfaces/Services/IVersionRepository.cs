using CalculateFunding.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces
{
    public interface IVersionRepository<T> where T: VersionedItem
    {
        T CreateVersion(T newVersion, T currentVersion = null);

        Task SaveVersion(T newVersion);

        Task<IEnumerable<T>> GetVersions(string sql = null, string partitionKeyId = null);

        Task<T> GetVersion(string entityId, int version);

        Task SaveVersions(IEnumerable<T> newVersions, int maxDegreesOfParallelism = 30);

        Task SaveVersions(IEnumerable<KeyValuePair<string, T>> newVersions, int maxDegreesOfParallelism = 30);

        Task<int> GetNextVersionNumber(T version, string partitionKeyId = null);
    }
}
