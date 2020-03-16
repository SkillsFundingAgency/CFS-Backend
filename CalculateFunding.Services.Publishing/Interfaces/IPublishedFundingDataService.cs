using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingDataService
    {
        Task<IEnumerable<PublishedProvider>> GetPublishedProvidersForApproval(string specificationId);

        Task<IEnumerable<PublishedProvider>> GetCurrentPublishedProviders(string fundingStreamId, string fundingPeriodId);

        Task<IEnumerable<PublishedFunding>> GetCurrentPublishedFunding(string fundingStreamId, string fundingPeriodId);

        Task<IEnumerable<string>> GetPublishedProviderFundingLines(string specificationId);
    }
}
