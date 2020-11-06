using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IProviderSourceDatasetsRepository
    {
        Task<Dictionary<string, Dictionary<string, ProviderSourceDataset>>> GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(string specificationId, IEnumerable<string> providerIds, IEnumerable<string> dataRelationshipIds);
    }
}
