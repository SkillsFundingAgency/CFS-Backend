using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IPublishedProviderCalculationResultsRepository
    {
        Task CreatePublishedCalculationResults(IEnumerable<PublishedProviderCalculationResult> publishedCalculationResults);

        Task<IEnumerable<PublishedProviderCalculationResultHistory>> GetPublishedProviderCalculationHistoryForSpecificationId(string specificationId);

        Task SavePublishedCalculationResultsHistory(IEnumerable<PublishedProviderCalculationResultHistory> publishedCalculationResultsHistory);

        Task<IEnumerable<PublishedProviderCalculationResult>> GetPublishedProviderCalculationResultsBySpecificationId(string specificationId);

        Task<IEnumerable<PublishedProviderCalculationResult>> GetPublishedProviderCalculationResultsBySpecificationIdAndProviderId(string specificationId, IEnumerable<string> providerIds);
    }
}
