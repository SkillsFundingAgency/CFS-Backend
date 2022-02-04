using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishService : IJobProcessingService
    {
        Task PublishProviderFundingResults(Message message, bool batched = false);
        Task PublishProviderFundingResults(bool batched, Reference author, string jobId, string correlationId, SpecificationSummary specification, PublishedProviderIdsRequest publishedProviderIdsRequest);
    }
}
