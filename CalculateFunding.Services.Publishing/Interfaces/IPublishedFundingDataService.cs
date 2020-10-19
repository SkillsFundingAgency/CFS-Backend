using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingDataService
    {
        Task<IEnumerable<PublishedProvider>> GetPublishedProvidersForApproval(string specificationId, string[] providerIds = null);

        Task<IEnumerable<PublishedProvider>> GetCurrentPublishedProviders(string fundingStreamId, string fundingPeriodId, string[] providerIds = null);

        Task<IEnumerable<PublishedFunding>> GetCurrentPublishedFunding(string fundingStreamId, string fundingPeriodId);

        Task<IEnumerable<string>> GetPublishedProviderFundingLines(string specificationId);

        Task<IEnumerable<PublishedFunding>> GetCurrentPublishedFunding(string specificationId, GroupingReason? groupingReason = null);
    }
}
