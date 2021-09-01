using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ProvidersForChannelFilterService : IProvidersForChannelFilterService
    {
        public IEnumerable<PublishedProviderVersion> FilterProvidersForChannel(Channel channel, IEnumerable<PublishedProviderVersion> publishedProviders, FundingConfiguration fundingConfiguration)
        {
            // TODO: Implement filtering based on the funding configuration

            foreach (PublishedProviderVersion provider in publishedProviders)
            {
                // Filter by provider type/subtype against funding config
                //provider.Provider.ProviderType
                //provider.Provider.ProviderSubType

                // Filter by provider status against funding config
                //provider.Provider.Status
            }

            return publishedProviders;
        }
    }
}
