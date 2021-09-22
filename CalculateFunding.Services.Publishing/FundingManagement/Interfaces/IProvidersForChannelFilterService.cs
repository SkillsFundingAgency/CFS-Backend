using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IProvidersForChannelFilterService
    {
        IEnumerable<PublishedProviderVersion> FilterProvidersForChannel(Channel channel, IEnumerable<PublishedProviderVersion> publishedProviders, FundingConfiguration fundingConfiguration);
    }
}