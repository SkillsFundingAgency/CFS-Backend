using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
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
        private readonly IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private readonly IGeneratePublishedFundingCsvJobsCreationLocator _generateCsvJobsLocator;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;

        public ApproveService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishedProviderIndexerService publishedProviderIndexerService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPrerequisiteCheckerLocator prerequisiteCheckerLocator,
            IJobManagement jobManagement,
            ILogger logger,
            ITransactionFactory transactionFactory,
            IPublishedProviderVersionService publishedProviderVersionService,
            IGeneratePublishedFundingCsvJobsCreationLocator generateCsvJobsLocator)
        {
            Guard.ArgumentNotNull(generateCsvJobsLocator, nameof(generateCsvJobsLocator));
            Guard.ArgumentNotNull(prerequisiteCheckerLocator, nameof(prerequisiteCheckerLocator));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishedProviderIndexerService, nameof(publishedProviderIndexerService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(transactionFactory, nameof(transactionFactory));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));

            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingDataService = publishedFundingDataService;
            _publishedProviderIndexerService = publishedProviderIndexerService;
            _prerequisiteCheckerLocator = prerequisiteCheckerLocator;
            _jobManagement = jobManagement;
            _logger = logger;
            _generateCsvJobsLocator = generateCsvJobsLocator;
            _transactionFactory = transactionFactory;
            _publishedProviderVersionService = publishedProviderVersionService;
        }

        public async Task ApproveBatchResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            _logger.Information("Starting approve provider funding job");
            string jobId = message.GetUserProperty<string>("jobId");

            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            await EnsureJobCanBeProcessed(jobId);

            // Update job to set status to processing
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, null, null);

            string specificationId = message.GetUserProperty<string>("specification-id");

            string approveProvidersRequestJson = message.GetUserProperty<string>(JobConstants.MessagePropertyNames.ApproveProvidersRequest);
            ApproveProvidersRequest approveProvidersRequest = JsonExtensions.AsPoco<ApproveProvidersRequest>(approveProvidersRequestJson);

            await PerformPrerequisiteChecks(specificationId, jobId, PrerequisiteCheckerType.ApproveBatchProviders, approveProvidersRequest?.Providers);
            
            _logger.Information($"Processing approve provider funding job. JobId='{jobId}'. SpecificationId='{specificationId}' Request={approveProvidersRequestJson}");

            IEnumerable<PublishedProvider> publishedProviders = await GetPublishedProvidersForApproval(specificationId, approveProvidersRequest.Providers.ToArray());

            CheckPublishedProviderForErrors(specificationId, publishedProviders);

            Reference author = message.GetUserDetails();
            string correlationId = message.GetUserProperty<string>("correlation-id");

            await ApproveProviders(publishedProviders, specificationId, jobId, author, correlationId);

            _logger.Information($"Completing approve provider funding job. JobId='{jobId}'");
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
            _logger.Information($"Approve provider funding job complete. JobId='{jobId}'");
        }

        public async Task ApproveAllResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            _logger.Information("Starting approve specification funding job");
            string jobId = message.GetUserProperty<string>("jobId");

            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            await EnsureJobCanBeProcessed(jobId);

            // Update job to set status to processing
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, null, null);

            string specificationId = message.GetUserProperty<string>("specification-id");

            await PerformPrerequisiteChecks(specificationId, jobId, PrerequisiteCheckerType.ApproveAllProviders);

            _logger.Information($"Processing approve specification funding job. JobId='{jobId}'. SpecificationId='{specificationId}'");

            _logger.Information("Fetching published providers for specification funding approval");

            IEnumerable<PublishedProvider> publishedProviders = await GetPublishedProvidersForApproval(specificationId);

            CheckPublishedProviderForErrors(specificationId, publishedProviders);

            Reference author = message.GetUserDetails();
            string correlationId = message.GetUserProperty<string>("correlation-id");

            await ApproveProviders(publishedProviders, specificationId, jobId, author, correlationId);

            _logger.Information($"Completing approve specification funding job. JobId='{jobId}'");
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
            _logger.Information($"Approve specification funding job complete. JobId='{jobId}'");
        }

        private async Task<IEnumerable<PublishedProvider>> GetPublishedProvidersForApproval(string specificationId, string[] providerIds = null)
        {
            _logger.Information("Fetching published providers for funding approval");

            Stopwatch existingPublishedProvidersStopwatch = Stopwatch.StartNew();
            IEnumerable<PublishedProvider> publishedProviders =
               await _publishedFundingDataService.GetPublishedProvidersForApproval(specificationId, providerIds);

            existingPublishedProvidersStopwatch.Stop();
            _logger.Information($"Fetched {publishedProviders.Count()} published providers for approval in {existingPublishedProvidersStopwatch.ElapsedMilliseconds} ms");

            return publishedProviders;
        }

        private async Task ApproveProviders(IEnumerable<PublishedProvider> publishedProviders, string specificationId, string jobId, Reference author, string correlationId)
        {
            string fundingPeriodId = publishedProviders.First().Current?.FundingPeriodId;

            using Transaction transaction = _transactionFactory.NewTransaction<ApproveService>();
            try
            {
                // if any error occurs while updating or indexing then we need to re-index all published providers for consistency
                transaction.Enroll(async () =>
                {
                    await _publishedProviderVersionService.CreateReIndexJob(author, correlationId);
                });

                _logger.Information($"Persisting new versions of published providers");
                if ((await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved, jobId, correlationId)) > 0)
                {
                    _logger.Information($"Indexing published providers");
                    await _publishedProviderIndexerService.IndexPublishedProviders(publishedProviders.Select(_ => _.Current));

                    _logger.Information("Creating generate Csv jobs");
                    IGeneratePublishedFundingCsvJobsCreation generateCsvJobs = _generateCsvJobsLocator
                        .GetService(GeneratePublishingCsvJobsCreationAction.Approve);
                    IEnumerable<string> fundingLineCodes = await _publishedFundingDataService.GetPublishedProviderFundingLines(specificationId);
                    IEnumerable<string> fundingStreamIds = Array.Empty<string>();
                    await generateCsvJobs.CreateJobs(specificationId, correlationId, author, fundingLineCodes, fundingStreamIds, fundingPeriodId);
                }

                transaction.Complete();
            }
            catch
            {
                await transaction.Compensate();

                throw;
            }
        }

        private static void CheckPublishedProviderForErrors(string specificationId, IEnumerable<PublishedProvider> publishedProviders)
        {
            if (publishedProviders.IsNullOrEmpty())
                throw new RetriableException($"Null or empty published providers returned for specification id : '{specificationId}' when setting status to approved.");

            if (publishedProviders.Any(_ => _.Current?.HasErrors == true))
            {
                throw new InvalidOperationException(
                    $"There are published providers with errors that must be fixed before they can be approved under specification {specificationId}.");
            }
        }

        private async Task EnsureJobCanBeProcessed(string jobId)
        {
            try
            {
                await _jobManagement.RetrieveJobAndCheckCanBeProcessed(jobId);
            }
            catch
            {
                string errorMessage = "Job can not be run";
                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }
        }

        private async Task PerformPrerequisiteChecks(string specificationId, string jobId, PrerequisiteCheckerType prerequisiteCheckerType, IEnumerable<string> providerIds = null)
        {
            _logger.Information($"Verifying prerequisites for {prerequisiteCheckerType}");

            IPrerequisiteChecker prerequisiteChecker = _prerequisiteCheckerLocator.GetPreReqChecker(prerequisiteCheckerType);
            await prerequisiteChecker.PerformChecks(specificationId, jobId, null, providerIds);

            _logger.Information($"Prerequisites for {prerequisiteCheckerType} passed");
        }
    }
}