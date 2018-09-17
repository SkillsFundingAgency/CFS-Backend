using CalculateFunding.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Versioning.Interfaces
{
    public interface IVersionRepository<T> where T: VersionedItem
    {
        Task SaveVersion(T newVersion, T currentVersion = null);

        Task<IEnumerable<T>> GetVersions(string entityId);
    }
}
