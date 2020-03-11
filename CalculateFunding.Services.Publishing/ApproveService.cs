using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class ApproveService : IApproveService
    {
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly IPublishedProviderIndexerService _publishedProviderIndexerService;
        private readonly IApprovePrerequisiteChecker _approvePrerequisiteChecker;
        private readonly IGeneratePublishedFundingCsvJobsCreationLocator _generateCsvJobsLocator;

        public ApproveService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishedProviderIndexerService publishedProviderIndexerService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IApprovePrerequisiteChecker approvePrerequisiteChecker,
            IJobManagement jobManagement,
            ILogger logger,
            IGeneratePublishedFundingCsvJobsCreationLocator generateCsvJobsLocator)
        {
            Guard.ArgumentNotNull(generateCsvJobsLocator, nameof(generateCsvJobsLocator));
            Guard.ArgumentNotNull(approvePrerequisiteChecker, nameof(approvePrerequisiteChecker));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishedProviderIndexerService, nameof(publishedProviderIndexerService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingDataService = publishedFundingDataService;
            _publishedProviderIndexerService = publishedProviderIndexerService;
            _approvePrerequisiteChecker = approvePrerequisiteChecker;
            _jobManagement = jobManagement;
            _logger = logger;
            _generateCsvJobsLocator = generateCsvJobsLocator;
        }

        public async Task ApproveResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            _logger.Information("Starting approve funding job");
            string jobId = message.GetUserProperty<string>("jobId");

            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            JobViewModel currentJob;
            try
            {
                currentJob = await _jobManagement.RetrieveJobAndCheckCanBeProcessed(jobId);
            }
            catch (Exception e)
            {
                string errorMessage = "Job can not be run";
                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }

            // Update job to set status to processing
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, null, null);

            Reference author = message.GetUserDetails();

            string specificationId = message.GetUserProperty<string>("specification-id");

            _logger.Information($"Verifying prerequisites for funding approval");

            await CheckPrerequisitesForSpecificationToBeApproved(specificationId, jobId);

            _logger.Information($"Prerequisites for approval passed");

            _logger.Information($"Processing approve funding job. JobId='{jobId}'. SpecificationId='{specificationId}'");

            _logger.Information("Fetching published providers for approval");

            Stopwatch existingPublishedProvidersStopwatch = Stopwatch.StartNew();
            IEnumerable<PublishedProvider> publishedProviders =
               await _publishedFundingDataService.GetPublishedProvidersForApproval(specificationId);

            existingPublishedProvidersStopwatch.Stop();
            _logger.Information($"Fetched {publishedProviders.Count()} published providers for approval in {existingPublishedProvidersStopwatch.ElapsedMilliseconds}ms");

            if (publishedProviders.IsNullOrEmpty())
                throw new RetriableException($"Null or empty published providers returned for specification id : '{specificationId}' when setting status to approved.");

            _logger.Information($"Persisting new versions of published providers");
            if ((await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved, jobId)) > 0)
            {
                _logger.Information($"Indexing published providers");
                await _publishedProviderIndexerService.IndexPublishedProviders(publishedProviders.Select(_ => _.Current));
            }
            
            string correlationId = message.GetUserProperty<string>("correlation-id");
            
            _logger.Information("Creating generate Csv jobs");

            IGeneratePublishedFundingCsvJobsCreation generateCsvJobs = _generateCsvJobsLocator
                .GetService(GeneratePublishingCsvJobsCreationAction.Approve);
            await generateCsvJobs.CreateJobs(specificationId, correlationId, author);

            _logger.Information($"Completing approve funding job. JobId='{jobId}'");
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
            _logger.Information($"Approve funding job complete. JobId='{jobId}'");
        }

        private async Task CheckPrerequisitesForSpecificationToBeApproved(string specificationId, string jobId)
        {
            IEnumerable<string> prereqValidationErrors = await _approvePrerequisiteChecker
                .PerformPrerequisiteChecks(specificationId);

            if (!prereqValidationErrors.IsNullOrEmpty())
            {
                string errorMessage = $"Specification with id: '{specificationId} has prerequisites which aren't complete.";

                await _jobManagement.UpdateJobStatus(jobId, completedSuccessfully: false, outcome: string.Join(", ", prereqValidationErrors));

                throw new NonRetriableException(errorMessage);
            }
        }
    }
}