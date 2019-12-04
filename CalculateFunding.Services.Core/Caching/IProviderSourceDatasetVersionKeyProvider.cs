using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Caching
{
    public interface IProviderSourceDatasetVersionKeyProvider
    {
        Task<Guid> GetProviderSourceDatasetVersionKey(string dataRelationshipId);
        
        Task AddOrUpdateProviderSourceDatasetVersionKey(string dataRelationshipId, Guid key);
    }
}