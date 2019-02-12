using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IPublishedProviderCalculationResultsRepository
    {
        Task CreatePublishedCalculationResults(IEnumerable<Migration.PublishedProviderCalculationResult> publishedCalculationResults);

        Task<IEnumerable<Migration.PublishedProviderCalculationResult>> GetPublishedProviderCalculationResultsBySpecificationId(string specificationId);

        Task<IEnumerable<Migration.PublishedProviderCalculationResult>> GetFundingOrPublicPublishedProviderCalculationResultsBySpecificationIdAndProviderId(string specificationId, IEnumerable<string> providerIds);

        Task<IEnumerable<PublishedProviderCalculationResultExisting>> GetExistingPublishedProviderCalculationResultsForSpecificationId(string specificationId);

        Task<Migration.PublishedProviderCalculationResult> GetPublishedProviderCalculationResultForId(string publishedProviderCalculationResultId, string providerId);

        Task<IEnumerable<Migration.PublishedProviderCalculationResultVersion>> GetPublishedCalculationVersions(string specificationId, string providerId);
    }
}
