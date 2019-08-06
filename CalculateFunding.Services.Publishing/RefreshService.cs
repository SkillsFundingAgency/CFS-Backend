using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshService : IRefreshService
    {
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly ISpecificationService _specificationService;
        private readonly IProviderService _providerService;
        private readonly ICalculationResultsRepository _calculationResultsRepository;
        private readonly IFundingLineGenerator _fundingLineGenerator;
        private readonly IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;
        private readonly IProfilingService _profilingService;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly ILogger _logger;

        private readonly Policy _publishingResiliencePolicy;
        private readonly Policy _jobsApiClientPolicy;

        public RefreshService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISpecificationService specificationService,
            IProviderService providerService,
            ICalculationResultsRepository calculationResultsRepository,
            IFundingLineGenerator fundingLineGenerator,
            IPublishedProviderContentsGeneratorResolver publishedProviderContentsGeneratorResolver,
            IProfilingService profilingService,
            IJobsApiClient jobsApiClient,
            ILogger logger)
        {
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(calculationResultsRepository, nameof(calculationResultsRepository));
            Guard.ArgumentNotNull(fundingLineGenerator, nameof(fundingLineGenerator));
            Guard.ArgumentNotNull(publishedProviderContentsGeneratorResolver, nameof(publishedProviderContentsGeneratorResolver));
            Guard.ArgumentNotNull(profilingService, nameof(profilingService));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));

            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingRepository = publishedFundingRepository;
            _specificationService = specificationService;
            _providerService = providerService;
            _calculationResultsRepository = calculationResultsRepository;
            _fundingLineGenerator = fundingLineGenerator;
            _publishedProviderContentsGeneratorResolver = publishedProviderContentsGeneratorResolver;
            _profilingService = profilingService;
            _jobsApiClient = jobsApiClient;
            _logger = logger;

            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _jobsApiClientPolicy = publishingResiliencePolicies.JobsApiClient;
        }

        public async Task<IEnumerable<Common.ApiClient.Providers.Models.Provider>> GetProvidersByProviderVersionId(string providerVersionId)
        {
            return await _providerService.GetProvidersByProviderVersionsId(providerVersionId);
        }

        public async Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId)
        {
            return await _specificationService.GetSpecificationSummaryById(specificationId);
        }

        public async Task RefreshResults(Message message)
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
            // Check this specification is published, if not publish it.
            // I am unable to find the service or methods in the common specifications client for this. The user stories haven't been done in this sprint yet.

            // Check the calculation engine is not running for this specification - fail job if it is

            // Get scoped providers for this specification
            IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders = await _providerService.GetProvidersByProviderVersionsId(specification.ProviderVersionId);

            // TODO: update job with number of providers * number of funding streams
            //             await UpdateJobStatus(jobId, TOTAL NUMBER OF ITEMS, 0, null, null);

            // Get existing published providers for this specification
            // TODO: Change to lookup per fundingstream for the specification to reduce load time and memory
            IEnumerable<PublishedProvider> existingPublishedProviders = await _publishingResiliencePolicy.ExecuteAsync(() =>
                _publishedFundingRepository.GetLatestPublishedProvidersBySpecification(specificationId));

            if (existingPublishedProviders.IsNullOrEmpty())
                throw new RetriableException(
                    $"Null or empty publsihed providers returned for specification id : '{specificationId}' when setting status to updated");

            // Get calculation results for specification
            IEnumerable<ProviderResult> allCalculationResults = await _calculationResultsRepository.GetCalculationResultsBySpecificationId(specificationId);
            Dictionary<string, IEnumerable<CalculationResult>> calculationResults = new Dictionary<string, IEnumerable<CalculationResult>>();
            foreach (var result in allCalculationResults)
            {
                calculationResults.Add(result.ProviderId, result.Results);
            }

            foreach (Reference fundingStream in specification.FundingStreams)
            {
                // TODO: Specifications service needs updating to store template per funding stream, rather than once

                TemplateMetadataContents templateMetadataContents = null; //await _policiesApiClient.GetFundingTemplateContents(fundingStreamId, specification.AssociatedTemplates[fundingStream.Id]);

                // Validate all of the calculations for this specification are mapped


                Dictionary<string, PublishedProvider> publishedProviders = new Dictionary<string, PublishedProvider>();
                foreach (PublishedProvider publishedProvider in existingPublishedProviders)
                {
                    if (publishedProvider.Current.FundingStreamId == fundingStream.Id)
                    {
                        publishedProviders.Add(publishedProvider.Current.ProviderId, publishedProvider);
                    }
                }

                // Create PublishedProvider for new providers
                Dictionary<string, PublishedProvider> newProviders = new Dictionary<string, PublishedProvider>();
                // TODO: Call service to create new PublishedProvider entities for this funding stream
                publishedProviders.AddRange(newProviders);

                // Calculate funding line totals
                Dictionary<string, IEnumerable<Models.Publishing.FundingLine>> fundingLineTotals = _fundingLineGenerator.GenerateFundingLines(templateMetadataContents, scopedProviders, allCalculationResults);

                // Profile payment funding lines
                await _profilingService.ProfileFundingLines(fundingLineTotals, fundingStream.Id, specification.FundingPeriod.Id);

                // Set payment funding line totals on each published provider

                // Determine changed providers which need saving
                Dictionary<string, PublishedProvider> publishedProvidersToUpdate = new Dictionary<string, PublishedProvider>();

                await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders.Values, author, PublishedProviderStatus.Updated);

                // Generate contents JSON for provider
                IPublishedProviderContentsGenerator generator = _publishedProviderContentsGeneratorResolver.GetService(templateMetadataContents.SchemaVersion);
                foreach (KeyValuePair<string, PublishedProvider> provider in publishedProvidersToUpdate)
                {
                    string contents = generator.GenerateContents(provider.Value.Current, templateMetadataContents, calculationResults[provider.Key], fundingLineTotals[provider.Key]);

                    // TODO Write contents to blob storage
                }

                // TODO Save updated PublishedProviders to cosmos
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