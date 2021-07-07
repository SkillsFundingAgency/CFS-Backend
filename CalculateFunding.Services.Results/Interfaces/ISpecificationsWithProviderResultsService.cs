using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.Results.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ISpecificationsWithProviderResultsService : IJobProcessingService
    {
        Task<IActionResult> GetSpecificationsWithProviderResultsForProviderId(string providerId);

        Task MergeSpecificationInformation(MergeSpecificationInformationRequest specificationInformation);

        Task<IActionResult> QueueMergeSpecificationInformationJob(MergeSpecificationInformationRequest mergeRequest,
            Reference user,
            string correlationId);
    }
}