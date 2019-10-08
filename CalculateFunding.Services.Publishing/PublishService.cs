using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using FundingConfiguration = CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig.FundingConfiguration;

namespace CalculateFunding.Services.Publishing
{
    public class PublishService : IPublishService
    {
        private readonly IPublishedFundingStatusUpdateService _publishedFundingStatusUpdateService;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly ISpecificationService _specificationService;

        private readonly IProviderService _providerService;

        private readonly IJobsApiClient _jobsApiClient;
        private readonly ILogger _logger;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IPublishPrerequisiteChecker _publishPrerequisiteChecker;
        private readonly IPublishedFundingChangeDetectorService _publishedFundingChangeDetectorService;
        private readonly IPublishedFundingGenerator _publishedFundingGenerator;
        private readonly IPublishedFundingContentsPersistanceService _publishedFundingContentsPersistanceService;
        private readonly IPublishedFundingDateService _publishedFundingDateService;
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly IOrganisationGroupGenerator _organisationGroupGenerator;
        private readonly ISearchRepository<PublishedFundingIndex> _publishedFundingSearchRepository;
        private readonly IPublishedProviderIndexerService _publishedProviderIndexerService;
        private readonly Policy _publishingResiliencePolicy;
        private readonly Policy _jobsApiClientPolicy;
        private readonly Policy _policyApiClientPolicy;

        public PublishService(IPublishedFundingStatusUpdateService publishedFundingStatusUpdateService,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISpecificationService specificationService,
            IOrganisationGroupGenerator organisationGroupGenerator,
            IPublishPrerequisiteChecker publishPrerequisiteChecker,
            IPublishedFundingChangeDetectorService publishedFundingChangeDetectorService,
            IPublishedFundingGenerator publishedFundingGenerator,
            IPublishedFundingContentsPersistanceService publishedFundingContentsPersistanceService,
            IPublishedFundingDateService publishedFundingDateService,
            IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IProviderService providerService,
            ISearchRepository<PublishedFundingIndex> publishedFundingSearchRepository,
            IPublishedProviderIndexerService publishedProviderIndexerService,
            IJobsApiClient jobsApiClient,
            IPoliciesApiClient policiesApiClient,
            ILogger logger
            )
        {
            Guard.ArgumentNotNull(publishedFundingStatusUpdateService, nameof(publishedFundingStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(organisationGroupGenerator, nameof(organisationGroupGenerator));
            Guard.ArgumentNotNull(publishPrerequisiteChecker, nameof(publishPrerequisiteChecker));
            Guard.ArgumentNotNull(publishedFundingChangeDetectorService, nameof(publishedFundingChangeDetectorService));
            Guard.ArgumentNotNull(publishedFundingGenerator, nameof(publishedFundingGenerator));
            Guard.ArgumentNotNull(publishedFundingContentsPersistanceService, nameof(publishedFundingContentsPersistanceService));
            Guard.ArgumentNotNull(publishedFundingDateService, nameof(publishedFundingDateService));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingSearchRepository, nameof(publishedFundingSearchRepository));
            Guard.ArgumentNotNull(publishedProviderIndexerService, nameof(publishedProviderIndexerService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies.JobsApiClient, nameof(publishingResiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PoliciesApiClient, nameof(publishingResiliencePolicies.PoliciesApiClient));

            _publishedFundingStatusUpdateService = publishedFundingStatusUpdateService;
            _publishedFundingDataService = publishedFundingDataService;
            _specificationService = specificationService;
            _organisationGroupGenerator = organisationGroupGenerator;
            _publishPrerequisiteChecker = publishPrerequisiteChecker;
            _publishedFundingChangeDetectorService = publishedFundingChangeDetectorService;
            _publishedFundingGenerator = publishedFundingGenerator;
            _publishedFundingContentsPersistanceService = publishedFundingContentsPersistanceService;
            _publishedFundingDateService = publishedFundingDateService;
            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _providerService = providerService;
            _publishedFundingSearchRepository = publishedFundingSearchRepository;
            _publishedProviderIndexerService = publishedProviderIndexerService;
            _jobsApiClient = jobsApiClient;
            _policiesApiClient = policiesApiClient;
            _logger = logger;

            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _jobsApiClientPolicy = publishingResiliencePolicies.JobsApiClient;
            _policyApiClientPolicy = publishingResiliencePolicies.PoliciesApiClient;
        }

        public async Task PublishResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            _logger.Information("Starting PublishFunding job");
            Reference author = message.GetUserDetails();

            string specificationId = message.UserProperties["specification-id"] as string;
            string jobId = message.UserProperties["jobId"]?.ToString();

            JobViewModel currentJob = await RetrieveJobAndCheckCanBeProcessed(jobId);
            if (currentJob == null)
            {
                string errorMessage = "Job can not be run";
                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }

            // Update job to set status to processing
            await UpdateJobStatus(jobId, 0, 0, null, null);

            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new NonRetriableException($"Could not find specification with id '{specificationId}'");
            }

            ApiResponse<Common.ApiClient.Policies.Models.FundingPeriod> fundingPeriod = await _policyApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingPeriodById(specification.FundingPeriod.Id));
            if (fundingPeriod.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Unable to lookup funding period from policy service");
            }
            if (fundingPeriod.Content == null)
            {
                throw new Exception("Unable to lookup funding period from policy service - content null");
            }

            PublishedFundingDates publishingDates = await _publishedFundingDateService.GetDatesForSpecification(specificationId);

            foreach (Reference fundingStream in specification.FundingStreams)
            {
                _logger.Information($"Processing Publish Funding for {fundingStream.Id} in specification {specificationId}");

                if (!specification.TemplateIds.ContainsKey(fundingStream.Id) || string.IsNullOrWhiteSpace(specification.TemplateIds[fundingStream.Id]))
                {
                    _logger.Information($"Skipped publishing {fundingStream.Id} as no template exists");

                    continue;
                }

                _logger.Information($"Retrieving published provider results for {fundingStream.Id} in specification {specificationId}");
                IEnumerable<PublishedProvider> publishedProvidersResult = await _publishedFundingDataService.GetCurrentPublishedProviders(fundingStream.Id, specification.FundingPeriod.Id);

                // Ensure linq query evaluates only once
                List<PublishedProvider> publishedProvidersForFundingStream = new List<PublishedProvider>(publishedProvidersResult);
                _logger.Information($"Retrieved {publishedProvidersForFundingStream.Count} published provider results for {fundingStream.Id}");


                if (publishedProvidersForFundingStream.IsNullOrEmpty())
                    throw new RetriableException(
                            $"Null or empty published providers returned for specification id : '{specificationId}' when setting status to released");

                _logger.Information($"Verifying prerequisites for funding publish");

                // Check prerequisites for this specification to be published
                IEnumerable<string> prereqValidationErrors = await _publishPrerequisiteChecker.PerformPrerequisiteChecks(specification, publishedProvidersForFundingStream);
                if (!prereqValidationErrors.IsNullOrEmpty())
                {
                    string errorMessage = $"Specification with id: '{specificationId} has prerequisites which aren't complete.";

                    await UpdateJobStatus(jobId, completedSuccessfully: false, outcome: string.Join(", ", prereqValidationErrors));

                    throw new NonRetriableException(errorMessage);
                }

                _logger.Information($"Prerequisites for publish passed");

                _logger.Information($"Fetching existing published funding");
                // Get latest version of existing published funding
                IEnumerable<PublishedFunding> publishedFunding = await _publishingResiliencePolicy.ExecuteAsync(() =>
                            _publishedFundingDataService.GetCurrentPublishedFunding(fundingStream.Id, specification.FundingPeriod.Id));

                _logger.Information($"Fetched {publishedFunding.Count()} existing published funding items");


                List<PublishedProvider> publishedProviders = publishedProvidersForFundingStream.Where(p => p.Current.FundingStreamId == fundingStream.Id).ToList();

                Dictionary<string, PublishedProvider> publishedProvidersToUpdate = new Dictionary<string, PublishedProvider>();

                ApiResponse<TemplateMetadataContents> templateMetadataContentsResponse = await _policiesApiClient.GetFundingTemplateContents(fundingStream.Id, specification.TemplateIds[fundingStream.Id]);

                if (templateMetadataContentsResponse?.Content == null)
                {
                    throw new NonRetriableException($"Unable to get template metadata contents for funding stream. '{fundingStream.Id}'");
                }

                TemplateMetadataContents templateMetadataContents = templateMetadataContentsResponse.Content;

                // Lookup the funding configuration to determine which groups to publish
                ApiResponse<FundingConfiguration> fundingConfigurationResponse = await _policyApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingConfiguration(fundingStream.Id, specification.FundingPeriod.Id));
                if (fundingConfigurationResponse.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new InvalidOperationException($"Unable to get funding configuration for funding stream '{fundingStream.Id}'");
                }

                FundingConfiguration fundingConfiguration = fundingConfigurationResponse.Content;

                IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders = await _providerService.GetScopedProvidersForSpecification(specificationId, specification.ProviderVersionId);

                List<string> publishedProviderProviderIds = publishedProviders.Select(p => p.Current.ProviderId).Distinct().ToList();

                // Filter scoped providers based on the PublishedProvider's which exist to support excluded PublishedProviders
                scopedProviders = scopedProviders.Where(p => publishedProviderProviderIds.Contains(p.ProviderId));


                _logger.Information($"Generating organisation groups");

                // Foreach group, determine the provider versions required to be latest
                IEnumerable<OrganisationGroupResult> organisationGroups = await _organisationGroupGenerator.GenerateOrganisationGroup(fundingConfiguration, scopedProviders, specification.ProviderVersionId);

                _logger.Information($"A total of {organisationGroups.Count()} were generated");

                _logger.Information($"Generating organisation groups to save");

                // Compare existing published provider versions with existing current PublishedFundingVersion
                IEnumerable<(PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> organisationGroupsToSave = _publishedFundingChangeDetectorService.GenerateOrganisationGroupsToSave(organisationGroups, publishedFunding, publishedProviders);

                _logger.Information($"A total of {organisationGroupsToSave.Count()} organisation groups returned to save");

                // Generate PublishedFundingVersion for new and updated PublishedFundings
                GeneratePublishedFundingInput generatePublishedFundingInput = new GeneratePublishedFundingInput()
                {
                    OrganisationGroupsToSave = organisationGroupsToSave,
                    TemplateMetadataContents = templateMetadataContents,
                    PublishedProviders = publishedProviders,
                    TemplateVersion = specification.TemplateIds[fundingStream.Id],
                    FundingStream = fundingStream,
                    FundingPeriod = fundingPeriod.Content,
                    PublishingDates = publishingDates,
                    SpecificationId = specification.Id,
                };

                _logger.Information($"Generating published funding");
                IEnumerable<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> publishedFundingToSave = _publishedFundingGenerator.GeneratePublishedFunding(generatePublishedFundingInput).ToList();
                _logger.Information($"A total of {publishedFundingToSave.Count()} published funding versions created to save.");

                // Save a version of published funding and set this version to current
                _logger.Information($"Saving published funding");
                await _publishedFundingStatusUpdateService.UpdatePublishedFundingStatus(publishedFundingToSave, author, PublishedFundingStatus.Released);
                _logger.Information($"Finished saving published funding");

                // Save contents to blob storage and search for the feed
                _logger.Information($"Saving published funding contents");
                await _publishedFundingContentsPersistanceService.SavePublishedFundingContents(publishedFundingToSave.Select(_ => _.PublishedFundingVersion), templateMetadataContents);
                _logger.Information($"Finished saving published funding contents");

                // Save published providers as published/released
                List<PublishedProvider> publishedProvidersToSaveAsReleased = new List<PublishedProvider>(publishedProviders.Where(p => p.Current.Status != PublishedProviderStatus.Released));
                _logger.Information($"Saving published providers. Total = '{publishedProvidersToSaveAsReleased.Count()}'");

                await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProvidersToSaveAsReleased, author, PublishedProviderStatus.Released, jobId);
                _logger.Information($"Finished saving published funding contents");

                // Update Published Providers in search 
                _logger.Information($"Updating published providers in search. Total='{publishedProvidersToSaveAsReleased.Count}'");
                await _publishedProviderIndexerService.IndexPublishedProviders(publishedProvidersToSaveAsReleased.Select(_ => _.Current));
                _logger.Information($"Finished updating published providers in search");

            }

            _logger.Information($"Running search reindexer for published funding");
            await _publishedFundingSearchRepository.RunIndexer();

            // Mark job as complete
            _logger.Information($"Marking publish funding job complete");

            await UpdateJobStatus(jobId, 0, 0, true, null);
            _logger.Information($"Publish funding job complete");
        }

        private async Task<JobViewModel> RetrieveJobAndCheckCanBeProcessed(string jobId)
        {
            ApiResponse<JobViewModel> response = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.GetJobById(jobId));

            if (response == null || response.Content == null)
            {
                _logger.Error($"Could not find the job with id: '{jobId}'");
                return null;
            }

            JobViewModel job = response.Content;

            if (job.CompletionStatus.HasValue)
            {
                _logger.Information($"Received job with id: '{jobId}' is already in a completed state with status {job.CompletionStatus.ToString()}");
                return null;
            }

            return job;
        }

        private async Task UpdateJobStatus(string jobId, int percentComplete = 0, bool? completedSuccessfully = null, string outcome = null)
        {
            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = completedSuccessfully,
                ItemsProcessed = percentComplete,
                Outcome = outcome
            };

            ApiResponse<JobLog> jobLogResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, jobLogUpdateModel));

            if (jobLogResponse == null || jobLogResponse.Content == null)
            {
                _logger.Error($"Failed to add a job log for job id '{jobId}'");
            }
        }

        private async Task UpdateJobStatus(string jobId, int totalItemsCount, int failedItemsCount, bool? completedSuccessfully = null, string outcome = null)
        {
            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = completedSuccessfully,
                ItemsProcessed = totalItemsCount,
                ItemsFailed = failedItemsCount,
                ItemsSucceeded = totalItemsCount - failedItemsCount,
                Outcome = outcome
            };

            ApiResponse<JobLog> jobLogResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, jobLogUpdateModel));

            if (jobLogResponse == null || jobLogResponse.Content == null)
            {
                _logger.Error($"Failed to add a job log for job id '{jobId}'");
            }
        }
    }
}