using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IPublishedProviderResultsAssemblerService
    {
        Task<IEnumerable<PublishedProviderResult>> AssemblePublishedProviderResults(IEnumerable<ProviderResult> providerResults, Reference author, SpecificationCurrentVersion specificationCurrentVersion);

        (IEnumerable<PublishedProviderCalculationResult>, bool) AssemblePublishedCalculationResults(IEnumerable<ProviderResult> providerResults, Reference author, SpecificationCurrentVersion specificationCurrentVersion, IEnumerable<PublishedProviderCalculationResultExisting> existingResults);

        /// <summary>
        /// Determines which PublishedProviderResults should be saved.
        /// If a result is not found in current results, then the value is set to 0
        /// </summary>
        /// <param name="providerResults">Published Provider Results from assembler</param>
        /// <param name="existingResults">Existing results stored in Cosmos</param>
        /// <returns>PublishedProviderResults to save, PublishedProviderResultExisting which do not exist in the current list</returns>
        Task<(IEnumerable<PublishedProviderResult>, IEnumerable<PublishedProviderResultExisting>)> GeneratePublishedProviderResultsToSave(IEnumerable<PublishedProviderResult> providerResults, IEnumerable<PublishedProviderResultExisting> existingResults, bool hasCalcsChanged = false);

        Task<IEnumerable<PublishedProviderCalculationResult>> GeneratePublishedProviderCalculationResultsToSave(IEnumerable<PublishedProviderCalculationResult> providerCalculationResults, IEnumerable<PublishedProviderCalculationResultExisting> existingResults);
    }
}
