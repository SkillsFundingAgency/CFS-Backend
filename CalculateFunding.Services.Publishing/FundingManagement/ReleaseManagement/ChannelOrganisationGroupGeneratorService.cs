using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Microsoft.FeatureManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ChannelOrganisationGroupGeneratorService : IChannelOrganisationGroupGeneratorService
    {
        private readonly IOrganisationGroupGenerator _organisationGroupGenerator;
        private readonly IMapper _mapper;
        private readonly IFeatureManager _featureManager;

        public ChannelOrganisationGroupGeneratorService(IOrganisationGroupGenerator organisationGroupGenerator,
            IMapper mapper,
            IFeatureManager featureManager)
        {
            _organisationGroupGenerator = organisationGroupGenerator;
            _mapper = mapper;
            _featureManager = featureManager;
        }

        public async Task<IEnumerable<OrganisationGroupResult>> GenerateOrganisationGroups(Channel channel,
            FundingConfiguration fundingConfiguration, SpecificationSummary specification,
            IEnumerable<PublishedProviderVersion> publishedProvidersInReleaseBatch)
        {
            IEnumerable<ApiProvider> batchProviders = _mapper.Map<IEnumerable<ApiProvider>>(publishedProvidersInReleaseBatch
                .Select(s => s.Provider));

            IEnumerable<OrganisationGroupingConfiguration> organisationGroupingConfigurations = fundingConfiguration
                .ReleaseChannels
                .SingleOrDefault(rc => rc.ChannelCode == channel.ChannelCode)
                ?.OrganisationGroupings;

            if (!organisationGroupingConfigurations.AnyWithNullCheck())
            {
                throw new InvalidOperationException($"The organisation group configuration is missing for channel with code '{channel.ChannelCode}'");
            }

            return await _organisationGroupGenerator.GenerateOrganisationGroup(
                organisationGroupingConfigurations,
                fundingConfiguration.ProviderSource,
                fundingConfiguration.PaymentOrganisationSource,
                batchProviders,
                specification.ProviderVersionId,
                specification.ProviderSnapshotId);
        }

        public async Task<IDictionary<string, IEnumerable<OrganisationGroupResult>>> GenerateOrganisationGroupsForAllChannels
            (FundingConfiguration fundingConfiguration, SpecificationSummary specification,
            IEnumerable<PublishedProviderVersion> publishedProvidersInReleaseBatch)
        {
            IEnumerable<ApiProvider> batchProviders = _mapper.Map<IEnumerable<ApiProvider>>(publishedProvidersInReleaseBatch
                .Select(s => s.Provider));

            Dictionary<string, IEnumerable<OrganisationGroupResult>> channelOrganisationGroups = new Dictionary<string, IEnumerable<OrganisationGroupResult>>();

            bool isChannelsEnabled = await _featureManager.IsEnabledAsync("EnableReleaseManagementBackend");

            if (!isChannelsEnabled || fundingConfiguration?.ReleaseChannels == null)
            {
                return channelOrganisationGroups;
            }

            foreach (FundingConfigurationChannel channel in fundingConfiguration.ReleaseChannels)
            {
                IEnumerable<OrganisationGroupingConfiguration> organisationGroupingConfigurations = channel?.OrganisationGroupings;
                channelOrganisationGroups.Add(channel.ChannelCode,
                    await _organisationGroupGenerator.GenerateOrganisationGroup(
                        organisationGroupingConfigurations,
                        fundingConfiguration.ProviderSource,
                        fundingConfiguration.PaymentOrganisationSource,
                        batchProviders,
                        specification.ProviderVersionId,
                        specification.ProviderSnapshotId)
                    );
            }

            return channelOrganisationGroups;
        }
    }
}
