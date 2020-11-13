using System.Collections.Concurrent;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.Results.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ISpecificationsWithProviderResultsService : IJobProcessingService
    {
        Task<IActionResult> GetSpecificationsWithProviderResultsForProviderId(string providerId);

        Task MergeSpecificationInformation(MergeSpecificationInformationRequest specificationInformation,
            ConcurrentDictionary<string, FundingPeriod> fundingPeriods);

        Task<IActionResult> QueueMergeSpecificationInformationJob(MergeSpecificationInformationRequest mergeRequest,
            Reference user,
            string correlationId);
    }
}