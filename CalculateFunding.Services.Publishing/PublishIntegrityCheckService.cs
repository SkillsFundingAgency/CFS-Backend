using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class PublishIntegrityCheckService : JobProcessingService, IPublishIntegrityCheckService
    {
        private readonly ILogger _logger;
        private readonly IPublishedFundingContentsPersistanceService _publishedFundingContentsPersistanceService;
        private readonly IPublishedProviderContentPersistanceService _publishedProviderContentsPersistanceService;
        private readonly ISpecificationService _specificationService;
        private readonly IProviderService _providerService;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly AsyncPolicy _calculationsApiClientPolicy;
        private readonly IPoliciesService _policiesService;
        private readonly IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;
        private readonly IPublishedFundingVersionDataService _publishedFundingVersionDataService;

        public PublishIntegrityCheckService(IJobManagement jobManagement,
            ILogger logger,
            ISpecificationService specificationService,
            IProviderService providerService,
            IPublishedFundingContentsPersistanceService publishedFundingContentsPersistanceService,
            IPublishedProviderContentPersistanceService publishedProviderContentsPersistanceService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishedFundingDataService publishedFundingDataService,
            IPoliciesService policiesService,
            ICalculationsApiClient calculationsApiClient,
            IPublishedFundingService publishedFundingService,
            IPublishedProviderContentsGeneratorResolver publishedProviderContentsGeneratorResolver,
            IPublishedFundingVersionDataService publishedFundingVersionDataService) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(publishedFundingContentsPersistanceService, nameof(publishedFundingContentsPersistanceService));
            Guard.ArgumentNotNull(publishedProviderContentsPersistanceService, nameof(publishedProviderContentsPersistanceService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies.CalculationsApiClient, nameof(publishingResiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(publishedFundingService, nameof(publishedFundingService));
            Guard.ArgumentNotNull(publishedProviderContentsGeneratorResolver, nameof(publishedProviderContentsGeneratorResolver));
            Guard.ArgumentNotNull(publishedFundingVersionDataService, nameof(publishedFundingVersionDataService));

            _logger = logger;
            _calculationsApiClient = calculationsApiClient;
            _calculationsApiClientPolicy = publishingResiliencePolicies.CalculationsApiClient;
            _specificationService = specificationService;
            _publishedFundingContentsPersistanceService = publishedFundingContentsPersistanceService;
            _publishedProviderContentsPersistanceService = publishedProviderContentsPersistanceService;
            _providerService = providerService;
            _publishedFundingDataService = publishedFundingDataService;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _policiesService = policiesService;
            _publishedProviderContentsGeneratorResolver = publishedProviderContentsGeneratorResolver;
            _publishedFundingVersionDataService = publishedFundingVersionDataService;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            Reference author = message.GetUserDetails();

            string specificationId = message.UserProperties["specification-id"] as string;
            bool publishAll = false;

            if (message.UserProperties.ContainsKey("publish-all"))
            {
                publishAll = bool.Parse(message.UserProperties["publish-all"].ToString());
            }

            IEnumerable<string> batchProviders = null;

            if (message.UserProperties.ContainsKey("providers-batch"))
            {
                batchProviders = JsonExtensions.AsPoco<IEnumerable<string>>(message.UserProperties["providers-batch"].ToString());
            }

            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new NonRetriableException($"Could not find specification with id '{specificationId}'");
            }
            
            foreach (Reference fundingStream in specification.FundingStreams)
            {
                (IDictionary<string, PublishedProvider> publishedProvidersForFundingStream,
                IDictionary<string, PublishedProvider> scopedPublishedProviders) = await _providerService.GetPublishedProviders(fundingStream,
                        specification);

                IDictionary<string, PublishedProvider> publishedProvidersByPublishedProviderId = publishedProvidersForFundingStream.Values.ToDictionary(_ => _.PublishedProviderId);

                IEnumerable<PublishedProvider> selectedPublishedProviders =
                batchProviders.IsNullOrEmpty() ?
                publishedProvidersForFundingStream.Values :
                batchProviders.Where(_ => publishedProvidersByPublishedProviderId.ContainsKey(_)).Select(_ => publishedProvidersByPublishedProviderId[_]);

                TemplateMapping templateMapping = await GetTemplateMapping(fundingStream, specification.Id);

                IEnumerable<PublishedFundingVersion> publishedFundingVersions = publishAll ?
                    await _publishedFundingVersionDataService.GetPublishedFundingVersion(fundingStream.Id, specification.FundingPeriod.Id) :
                    (await _publishingResiliencePolicy.ExecuteAsync(() =>
                    _publishedFundingDataService.GetCurrentPublishedFunding(fundingStream.Id, specification.FundingPeriod.Id))).Select(_ => _.Current);

                TemplateMetadataContents templateMetadataContents = await _policiesService.GetTemplateMetadataContents(fundingStream.Id, specification.FundingPeriod.Id, specification.TemplateIds[fundingStream.Id]);

                if (templateMetadataContents == null)
                {
                    throw new NonRetriableException($"Unable to get template metadata contents for funding stream. '{fundingStream.Id}'");
                }

                // Save contents to blob storage and search for the feed
                _logger.Information($"Saving published funding contents");
                await _publishedFundingContentsPersistanceService.SavePublishedFundingContents(publishedFundingVersions,
                    templateMetadataContents);
                _logger.Information($"Finished saving published funding contents");

                // Generate contents JSON for provider and save to blob storage
                IPublishedProviderContentsGenerator generator = _publishedProviderContentsGeneratorResolver.GetService(templateMetadataContents.SchemaVersion);
                await _publishedProviderContentsPersistanceService.SavePublishedProviderContents(templateMetadataContents, templateMapping,
                    selectedPublishedProviders, generator, publishAll);
            }
        }

        private async Task<TemplateMapping> GetTemplateMapping(Reference fundingStream, string specificationId)
        {
            ApiResponse<TemplateMapping> calculationMappingResult =
                await _calculationsApiClientPolicy.ExecuteAsync(() => _calculationsApiClient.GetTemplateMapping(specificationId, fundingStream.Id));

            if (calculationMappingResult == null)
            {
                throw new Exception($"calculationMappingResult returned null for funding stream {fundingStream.Id}");
            }

            return calculationMappingResult.Content;
        }
    }
}
