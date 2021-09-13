using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ProvidersForChannelFilterService : IProvidersForChannelFilterService
    {
        public IEnumerable<PublishedProviderVersion> FilterProvidersForChannel(Channel channel, IEnumerable<PublishedProviderVersion> publishedProviders, FundingConfiguration fundingConfiguration)
        {
            Guard.ArgumentNotNull(channel, nameof(channel));
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));
            Guard.ArgumentNotNull(fundingConfiguration, nameof(fundingConfiguration));

            FundingConfigurationChannel fundingConfigurationChannel
                = fundingConfiguration.ReleaseChannels.SingleOrDefault(_ => _.ChannelCode == channel.ChannelCode);

            if (fundingConfigurationChannel == null)
            {
                return publishedProviders;
            }

            IEnumerable<PublishedProviderVersion> publishedProviderVersionsForGroup = fundingConfigurationChannel.ProviderTypeMatch.IsNullOrEmpty() ?
                publishedProviders :
                publishedProviders.Where(_ => ShouldIncludeProvider(_.Provider, fundingConfigurationChannel.ProviderTypeMatch));

            publishedProviderVersionsForGroup = fundingConfigurationChannel.ProviderStatus.IsNullOrEmpty() ?
                publishedProviderVersionsForGroup : publishedProviderVersionsForGroup.Where(_ => ShouldIncludeProvider(_.Provider, fundingConfigurationChannel.ProviderStatus));

            return publishedProviderVersionsForGroup;
        }

        private bool ShouldIncludeProvider(Provider provider, IEnumerable<ProviderTypeMatch> providerTypeMatches)
            => providerTypeMatches.Any(providerTypeMatch => string.Equals(provider.ProviderType, providerTypeMatch.ProviderType, StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(provider.ProviderSubType, providerTypeMatch.ProviderSubtype, StringComparison.InvariantCultureIgnoreCase));

        private bool ShouldIncludeProvider(Provider provider, IEnumerable<string> providerStatus) =>
            providerStatus.Any(status => string.Equals(provider.Status, status, StringComparison.InvariantCultureIgnoreCase));
    }
}
