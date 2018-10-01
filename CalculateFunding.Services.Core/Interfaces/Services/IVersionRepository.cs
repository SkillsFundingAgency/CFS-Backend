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

        Task<IEnumerable<T>> GetVersions(string entityId);

        Task<T> GetVersion(string entityId, int version);

        Task SaveVersions(IEnumerable<T> newVersions);

        int GetNextVersionNumber(T version);
    }
}
