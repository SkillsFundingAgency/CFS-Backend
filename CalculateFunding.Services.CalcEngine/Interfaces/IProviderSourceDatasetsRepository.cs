using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IProviderSourceDatasetsRepository
    {
        Task<ProviderSourceDatasetLookupResult> GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(string specificationId, IEnumerable<string> providerIds, IEnumerable<string> dataRelationshipIds);
    }
}
