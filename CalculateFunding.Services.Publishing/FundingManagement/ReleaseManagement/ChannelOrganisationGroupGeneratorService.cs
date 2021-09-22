using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
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

        public ChannelOrganisationGroupGeneratorService(IOrganisationGroupGenerator organisationGroupGenerator,
            IMapper mapper)
        {
            _organisationGroupGenerator = organisationGroupGenerator;
            _mapper = mapper;
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

            return await _organisationGroupGenerator.GenerateOrganisationGroup(
                organisationGroupingConfigurations,
                fundingConfiguration.ProviderSource,
                fundingConfiguration.PaymentOrganisationSource,
                batchProviders,
                specification.ProviderVersionId,
                specification.ProviderSnapshotId);
        }
    }
}
