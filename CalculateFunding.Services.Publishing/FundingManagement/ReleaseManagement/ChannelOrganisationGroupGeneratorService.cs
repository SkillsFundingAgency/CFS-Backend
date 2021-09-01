using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
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

        public async Task<IEnumerable<OrganisationGroupResult>> GenerateOrganisationGroups(Channel channel, FundingConfiguration fundingConfiguration, SpecificationSummary specification, IEnumerable<PublishedProviderVersion> publishedProvidersInReleaseBatch)
        {
            // Convert publishedProvider.Provider to the API client Provider model to pass in below:
            // There may be an existing automapper config for this

            IEnumerable<ApiProvider> batchProviders = null;

            // TODO: Update GenerateOrganisationGroup to ensure the per channel groups are generated, rather than everything at funding config level.
            // Pass in OrganisationGroupingConfiguration from FundingConfiguration ReleaseChannels associated with this channel and PaymentSource seperately
            // Update OrganisationGroupGeneration in common to support this

            return await _organisationGroupGenerator.GenerateOrganisationGroup(fundingConfiguration, batchProviders, specification.ProviderVersionId, specification.ProviderSnapshotId);
        }
    }
}
