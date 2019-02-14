using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Users;
using CalculateFunding.Repositories.Common.Search.Results;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IProviderResultsRepository
    {
        Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId);

        Task<HttpStatusCode> UpdateProviderResults(IEnumerable<ProviderResult> providerResults, UserProfile userProfile);

        Task<ProviderSearchResults> SearchProviders(SearchModel searchModel);

       // Task<IEnumerable<ProviderSummary>> GetAllProviderSummaries();

        Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdAndSpecificationId(string providerId, string specificationId);

        // Task<int> PartitionProviderSummaries(int partitionSize);

        Task<IEnumerable<ProviderSummary>> LoadAllProvidersFromSearch();

        Task<IEnumerable<string>> GetScopedProviderIds(string specificationId);

        Task<int> PopulateProviderSummariesForSpecification(string specificationId);
    }
}
