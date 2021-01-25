using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Core;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingService : IPublishedFundingService
    {
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly IPoliciesService _policiesService;
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly IOrganisationGroupGenerator _organisationGroupGenerator;
        private readonly IPublishedFundingChangeDetectorService _publishedFundingChangeDetectorService;
        private readonly IPublishedFundingDateService _publishedFundingDateService;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public PublishedFundingService(IPublishedFundingDataService publishedFundingDataService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPoliciesService policiesService,
            IOrganisationGroupGenerator organisationGroupGenerator,
            IPublishedFundingChangeDetectorService publishedFundingChangeDetectorService,
            IPublishedFundingDateService publishedFundingDateService,
            IMapper mapper,
            ILogger logger)
        {
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingChangeDetectorService, nameof(publishedFundingChangeDetectorService));
            Guard.ArgumentNotNull(publishedFundingDateService, nameof(publishedFundingDateService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _publishedFundingDataService = publishedFundingDataService;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _policiesService = policiesService;
            _organisationGroupGenerator = organisationGroupGenerator;
            _publishedFundingChangeDetectorService = publishedFundingChangeDetectorService;
            _publishedFundingDateService = publishedFundingDateService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<PublishedFundingInput> GeneratePublishedFundingInput(IDictionary<string, PublishedProvider> publishedProvidersForFundingStream,
            IEnumerable<Provider> scopedProviders,
            Reference fundingStream,
            SpecificationSummary specification,
            IEnumerable<PublishedProvider> publishedProvidersInScope)
        {
            Guard.ArgumentNotNull(publishedProvidersForFundingStream, nameof(publishedProvidersForFundingStream));
            Guard.ArgumentNotNull(scopedProviders, nameof(scopedProviders));
            Guard.ArgumentNotNull(fundingStream, nameof(fundingStream));
            Guard.ArgumentNotNull(specification, nameof(specification));

            _logger.Information($"Fetching existing published funding");

            // Get latest version of existing published funding
            IEnumerable <PublishedFunding> publishedFunding = await _publishingResiliencePolicy.ExecuteAsync(() =>
                _publishedFundingDataService.GetCurrentPublishedFunding(fundingStream.Id, specification.FundingPeriod.Id));

            _logger.Information($"Fetched {publishedFunding.Count()} existing published funding items");

            _logger.Information($"Generating organisation groups");

            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStream.Id, specification.FundingPeriod.Id);

            TemplateMetadataContents templateMetadataContents = await ReadTemplateMetadataContents(fundingStream, specification);

            // Foreach group, determine the provider versions required to be latest
            IEnumerable<OrganisationGroupResult> organisationGroups =
                await _organisationGroupGenerator.GenerateOrganisationGroup(fundingConfiguration, _mapper.Map<IEnumerable<ApiProvider>>(scopedProviders), specification.ProviderVersionId, specification.ProviderSnapshotId);

            // filter out organisation groups which don't contain a provider which is in scope
            if (!publishedProvidersInScope.IsNullOrEmpty())
            {
                HashSet<string> publishedProviderIdsInScope = new HashSet<string>(publishedProvidersInScope.DistinctBy(_ => _.Current.ProviderId).Select(_ => _.Current.ProviderId));
                organisationGroups = organisationGroups.Where(_ => _.Providers.Any(provider => publishedProviderIdsInScope.Contains(provider.ProviderId)));
            }

            _logger.Information($"A total of {organisationGroups.Count()} were generated");

            _logger.Information($"Generating organisation groups to save");

            // Compare existing published provider versions with existing current PublishedFundingVersion
            IEnumerable<(PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> organisationGroupsToSave =
                _publishedFundingChangeDetectorService.GenerateOrganisationGroupsToSave(organisationGroups, publishedFunding, publishedProvidersForFundingStream);

            _logger.Information($"A total of {organisationGroupsToSave.Count()} organisation groups returned to save");

            // Generate PublishedFundingVersion for new and updated PublishedFundings
            return new PublishedFundingInput()
            {
                OrganisationGroupsToSave = organisationGroupsToSave,
                TemplateMetadataContents = templateMetadataContents,
                TemplateVersion = specification.TemplateIds[fundingStream.Id],
                FundingStream = fundingStream,
                FundingPeriod = await _policiesService.GetFundingPeriodByConfigurationId(specification.FundingPeriod.Id),
                PublishingDates = await _publishedFundingDateService.GetDatesForSpecification(specification.Id),
                SpecificationId = specification.Id,
            };
        }

        private async Task<TemplateMetadataContents> ReadTemplateMetadataContents(Reference fundingStream, SpecificationSummary specification)
        {
            TemplateMetadataContents templateMetadataContents =
                await _policiesService.GetTemplateMetadataContents(fundingStream.Id, specification.FundingPeriod.Id, specification.TemplateIds[fundingStream.Id]);

            if (templateMetadataContents == null)
            {
                throw new NonRetriableException($"Unable to get template metadata contents for funding stream. '{fundingStream.Id}'");
            }

            return templateMetadataContents;
        }
    }
}
