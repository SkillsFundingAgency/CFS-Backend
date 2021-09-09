using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingConfigurationChannelBuilder : TestEntityBuilder
    {
        private string _channelCode;
        private IEnumerable<string> _providerStatus;
        private IEnumerable<ProviderTypeMatch> _providerTypeMatch;
        private IEnumerable<OrganisationGroupingConfiguration> _organisationGroupings;

        public FundingConfigurationChannelBuilder WithChannelCode(string channelCode)
        {
            _channelCode = channelCode;
            return this;
        }

        public FundingConfigurationChannelBuilder WithProviderStatus(params string[] providerStatus)
        {
            _providerStatus = providerStatus;
            return this;
        }

        public FundingConfigurationChannelBuilder WithProviderTypeMatch(params ProviderTypeMatch[] providerTypeMatch)
        {
            _providerTypeMatch = providerTypeMatch;
            return this;
        }

        public FundingConfigurationChannelBuilder WithOrganisationGroupings(params OrganisationGroupingConfiguration[] organisationGroupings)
        {
            _organisationGroupings = organisationGroupings;
            return this;
        }

        public FundingConfigurationChannel Build()
            => new FundingConfigurationChannel()
            {
                ChannelCode = _channelCode ?? NewRandomString(),
                ProviderStatus = _providerStatus ?? Array.Empty<string>(),
                ProviderTypeMatch = _providerTypeMatch ?? Array.Empty<ProviderTypeMatch>(),
                OrganisationGroupings = _organisationGroupings ?? Array.Empty<OrganisationGroupingConfiguration>()
            };
    }
}
