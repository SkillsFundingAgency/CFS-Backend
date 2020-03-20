using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingOrganisationGroupingService : IPublishedFundingOrganisationGroupingService
    {
        private readonly IPoliciesService _policiesService;
        private readonly IProviderService _providerService;
        private readonly IOrganisationGroupGenerator _organisationGroupGenerator;
        private readonly IMapper _mapper;
        private readonly IPublishedFundingChangeDetectorService _publishedFundingChangeDetectorService;

        public PublishedFundingOrganisationGroupingService(
            IPoliciesService policiesService,
            IProviderService providerService,
            IOrganisationGroupGenerator organisationGroupGenerator,
            IMapper mapper,
            IPublishedFundingChangeDetectorService publishedFundingChangeDetectorService
            )
        {
            Guard.ArgumentNotNull(organisationGroupGenerator, nameof(organisationGroupGenerator));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(publishedFundingChangeDetectorService, nameof(publishedFundingChangeDetectorService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));

            _organisationGroupGenerator = organisationGroupGenerator;
            _providerService = providerService;
            _mapper = mapper;
            _publishedFundingChangeDetectorService = publishedFundingChangeDetectorService;
            _policiesService = policiesService;
        }

        public async Task<IEnumerable<PublishedFundingOrganisationGrouping>> GeneratePublishedFundingOrganisationGrouping(
            bool includeHistory, 
            string fundingStreamId,
            SpecificationSummary specification,
            IEnumerable<PublishedFundingVersion> publishedFundingVersions)
        {
            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStreamId, specification.FundingPeriod.Id);

            Reference fundingStream = new Reference { Id = fundingStreamId };
            (IDictionary<string, PublishedProvider> publishedProvidersForFundingStream, IDictionary<string, PublishedProvider> scopedPublishedProviders) =
                await _providerService.GetPublishedProviders(fundingStream, specification);

            IEnumerable<Provider> scopedProviders = scopedPublishedProviders?.Values.Select(_ => _.Current.Provider);

            IEnumerable<OrganisationGroupResult> organisationGroups =
                await _organisationGroupGenerator.GenerateOrganisationGroup(fundingConfiguration, _mapper.Map<IEnumerable<ApiProvider>>(scopedProviders), specification.ProviderVersionId);

            IEnumerable<PublishedFundingOrganisationGrouping> organisationGroupings =
                _publishedFundingChangeDetectorService.GenerateOrganisationGroupings(organisationGroups, publishedFundingVersions, publishedProvidersForFundingStream, includeHistory);

            return organisationGroupings;
        }
    }
}
