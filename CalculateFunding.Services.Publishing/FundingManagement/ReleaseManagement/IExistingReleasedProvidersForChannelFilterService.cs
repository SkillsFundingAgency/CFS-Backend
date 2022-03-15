using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public interface IExistingReleasedProvidersForChannelFilterService
    {
        IEnumerable<PublishedProviderVersion> FilterExistingReleasedProviderInChannel(IEnumerable<PublishedProviderVersion> providersInGroupsToRelease, IEnumerable<ProviderVersionInChannel> latestProviderVersionsInChannel, IEnumerable<string> batchPublishedProviderIds, int channelId, string specificationId);
    }
}