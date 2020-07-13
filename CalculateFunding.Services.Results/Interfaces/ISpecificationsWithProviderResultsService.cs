using System.Collections.Concurrent;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Results.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ISpecificationsWithProviderResultsService
    {
        Task<IActionResult> GetSpecificationsWithProviderResultsForProviderId(string providerId);

        Task MergeSpecificationInformation(Message message);

        Task<IActionResult> QueueMergeSpecificationInformationForProviderJob(SpecificationInformation specificationInformation,
            Reference user,
            string correlationId,
            string providerId = null);

        Task MergeSpecificationInformation(SpecificationInformation specificationInformation,
            string providerId,
            ConcurrentDictionary<string, FundingPeriod> fundingPeriods);
    }
}