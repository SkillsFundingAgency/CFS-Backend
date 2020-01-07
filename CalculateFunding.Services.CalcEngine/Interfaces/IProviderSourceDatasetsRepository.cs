using CalculateFunding.Models.Datasets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IProviderSourceDatasetsRepository
    {
        Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(IEnumerable<string> providerIds, IEnumerable<string> dataRelationshipIds);
    }
}
