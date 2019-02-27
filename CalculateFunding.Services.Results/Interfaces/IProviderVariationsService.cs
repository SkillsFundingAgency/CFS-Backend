using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderVariationsService
    {
        Task<ProcessProviderVariationsResult> ProcessProviderVariations(JobViewModel triggeringJob, SpecificationCurrentVersion specification, IEnumerable<ProviderResult> providerResults, IEnumerable<PublishedProviderResultExisting> existingPublishedProviderResults, IEnumerable<PublishedProviderResult> allPublishedProviderResults, List<PublishedProviderResult> resultsToSave, Reference author);
    }
}