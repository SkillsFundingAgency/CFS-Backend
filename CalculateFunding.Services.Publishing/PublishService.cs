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
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;
using FundingConfiguration = CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig.FundingConfiguration;

namespace CalculateFunding.Services.Publishing
{
    public class PublishService : IPublishService
    {
        private readonly IPublishedFundingStatusUpdateService _publishedFundingStatusUpdateService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly ISpecificationService _specificationService;

        private readonly IProviderService _providerService;

        private readonly IJobsApiClient _jobsApiClient;
        private readonly ILogger _logger;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IPublishPrerequisiteChecker _publishPrerequisiteChecker;
        private readonly IPublishedFundingChangeDetectorService _publishedFundingChangeDetectorService;
        private readonly IPublishedFundingGenerator _publishedFundingGenerator;
        private readonly IPublishedFundingContentsPersistanceService _publishedFundingContentsPersistanceService;

        // TODO: Change to IOrganisationGroupGenerator once common is updated
        private readonly OrganisationGroupGenerator _organisationGroupGenerator;


        private readonly Policy _publishingResiliencePolicy;
        private readonly Policy _jobsApiClientPolicy;
        private readonly Policy _policyApiClientPolicy;

        public PublishService(IPublishedFundingStatusUpdateService publishedFundingStatusUpdateService,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISpecificationService specificationService,
            // TODO - update this to interface once common package version is updated
            OrganisationGroupGenerator organisationGroupGenerator,
            IPublishPrerequisiteChecker publishPrerequisiteChecker,
            IPublishedFundingChangeDetectorService publishedFundingChangeDetectorService,
            IPublishedFundingGenerator publishedFundingGenerator,
            IPublishedFundingContentsPersistanceService publishedFundingContentsPersistanceService,
            IProviderService providerService,
            IJobsApiClient jobsApiClient,
            IPoliciesApiClient policiesApiClient,
            ILogger logger
            )
        {
            Guard.ArgumentNotNull(publishedFundingStatusUpdateService, nameof(publishedFundingStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(organisationGroupGenerator, nameof(organisationGroupGenerator));
            Guard.ArgumentNotNull(publishPrerequisiteChecker, nameof(publishPrerequisiteChecker));
            Guard.ArgumentNotNull(publishedFundingChangeDetectorService, nameof(publishedFundingChangeDetectorService));
            Guard.ArgumentNotNull(publishedFundingGenerator, nameof(publishedFundingGenerator));
            Guard.ArgumentNotNull(publishedFundingContentsPersistanceService, nameof(publishedFundingContentsPersistanceService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _publishedFundingStatusUpdateService = publishedFundingStatusUpdateService;
            _publishedFundingRepository = publishedFundingRepository;
            _specificationService = specificationService;
            _organisationGroupGenerator = organisationGroupGenerator;
            _publishPrerequisiteChecker = publishPrerequisiteChecker;
            _publishedFundingChangeDetectorService = publishedFundingChangeDetectorService;
            _publishedFundingGenerator = publishedFundingGenerator;
            _publishedFundingContentsPersistanceService = publishedFundingContentsPersistanceService;
            _providerService = providerService;
            _jobsApiClient = jobsApiClient;
            _policiesApiClient = policiesApiClient;
            _logger = logger;

            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _jobsApiClientPolicy = publishingResiliencePolicies.JobsApiClient;
            _policyApiClientPolicy = publishingResiliencePolicies.PoliciesApiClient;
        }

        public async Task<ApiSpecificationSummary> GetSpecificationSummaryById(string specificationId)
        {
            return await _specificationService.GetSpecificationSummaryById(specificationId);
        }

        public async Task PublishResults(Message message)
        {
            //Ignore this for now in the pr, its just place holder stuff for the next stories
            //We will be getting the job if from the message and the spec id
            //We will be adding telemtry
            //Updating cache with percentage comeplete
            //and whatever else

            Guard.ArgumentNotNull(message, nameof(message));

            Reference author = message.GetUserDetails();

            string specificationId = message.UserProperties["specificationId"] as string;
            string jobId = message.UserProperties["jobId"]?.ToString();

            JobViewModel currentJob = await RetrieveJobAndCheckCanBeProcessed(jobId);
            if (currentJob == null)
            {
                throw new NonRetriableException("Job can not be run");
            }

            // Update job to set status to processing
            await UpdateJobStatus(jobId, 0, 0, null, null);

            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new NonRetriableException($"Could not find specification with id '{specificationId}'");
            }

            IEnumerable<PublishedProvider> publishedProviders = await _publishingResiliencePolicy.ExecuteAsync(() =>
                        _publishedFundingRepository.GetLatestPublishedProvidersBySpecification(specificationId));

            if (publishedProviders.IsNullOrEmpty())
                throw new RetriableException(
                        $"Null or empty published providers returned for specification id : '{specificationId}' when setting status to released");


            // Check prerequisites for this specification to be published
            IEnumerable<string> prereqValidationErrors = await _publishPrerequisiteChecker.PerformPrerequisiteChecks(specification, publishedProviders);
            if (!prereqValidationErrors.IsNullOrEmpty())
            {
                string errorMessage = $"Specification with id: '{specificationId} has prerequisites which aren't complete.";

                await UpdateJobStatus(jobId, completedSuccessfully: false, outcome: string.Join(", ", prereqValidationErrors));

                throw new NonRetriableException(errorMessage);
            }


            // Get latest version of existing published funding
            IEnumerable<PublishedFunding> publishedFunding = await _publishingResiliencePolicy.ExecuteAsync(() =>
                        _publishedFundingRepository.GetLatestPublishedFundingBySpecification(specificationId));


            foreach (Reference fundingStream in specification.FundingStreams)
            {
                Dictionary<string, PublishedProvider> publishedProvidersToUpdate = new Dictionary<string, PublishedProvider>();

                ApiResponse<TemplateMetadataContents> templateMetadataContentsResponse = await _policiesApiClient.GetFundingTemplateContents(fundingStream.Id, specification.TemplateIds[fundingStream.Id]);
                // TODO: Null and response checking on response. If there is a null associated template, continue to next funding stream
                TemplateMetadataContents templateMetadataContents = templateMetadataContentsResponse.Content;

                // Lookup the funding configuration to determine which groups to publish
                var fundingConfigurationResponse = await _policyApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingConfiguration(fundingStream.Id, specification.FundingPeriod.Id));
                FundingConfiguration fundingConfiguration = fundingConfigurationResponse.Content;

                IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders = await _providerService.GetScopedProvidersForSpecification(specificationId, specification.ProviderVersionId);

                // Foreach group, determine the provider versions required to be latest
                IEnumerable<OrganisationGroupResult> organisationGroups = await _organisationGroupGenerator.GenerateOrganisationGroup(fundingConfiguration, scopedProviders, specification.ProviderVersionId);

                // Compare existing published provider versions with existing current PublishedFundingVersion
                IEnumerable<OrganisationGroupResult> organisationGroupsToSave = await _publishedFundingChangeDetectorService.GenerateOrganisationGroupsToSave(organisationGroups, publishedFunding, publishedProviders);

                // Generate PublishedFundingVersion for new and updated PublishedFundings
                IEnumerable<PublishedFundingVersion> publishedFundingToSave = _publishedFundingGenerator.GeneratePublishedFunding(organisationGroupsToSave, templateMetadataContents, publishedProviders);

                // Save a version of published funding and set this version to current
                await _publishedFundingStatusUpdateService.UpdatePublishedFundingStatus(publishedFundingToSave, author, PublishedProviderStatus.Released);

                // Save contents to blob storage and search for the feed
                await _publishedFundingContentsPersistanceService.SavePublishedFundingContents(publishedFundingToSave, templateMetadataContents);
            }

            // Mark job as complete
            await UpdateJobStatus(jobId, 0, 0, true, null);
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