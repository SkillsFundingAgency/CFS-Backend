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
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class ApproveService : JobProcessingService, IApproveService
    {
        private const string SfaCorrelationId = "sfa-correlationId";

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
            IGeneratePublishedFundingCsvJobsCreationLocator generateCsvJobsLocator) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(generateCsvJobsLocator, nameof(generateCsvJobsLocator));
            Guard.ArgumentNotNull(prerequisiteCheckerLocator, nameof(prerequisiteCheckerLocator));
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
            _logger = logger;
            _generateCsvJobsLocator = generateCsvJobsLocator;
            _transactionFactory = transactionFactory;
            _publishedProviderVersionService = publishedProviderVersionService;
        }

        public override async Task Process(Message message)
        {
            await ApproveResults(message);
        }

        public async Task ApproveResults(Message message, bool batched = false)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            _logger.Information("Starting approve provider funding job");
            string specificationId = message.GetUserProperty<string>("specification-id");

            PublishedProviderIdsRequest publishedProviderIdsRequest = null;

            string logApproveProcessingMessage = $"Processing approve specification funding job. JobId='{Job.Id}'. SpecificationId='{specificationId}'.";

            if (batched)
            {
                string publishedProviderIdsRequestJson = message.GetUserProperty<string>(JobConstants.MessagePropertyNames.PublishedProviderIdsRequest);
                logApproveProcessingMessage += $" Request = {publishedProviderIdsRequestJson}.";
                publishedProviderIdsRequest = JsonExtensions.AsPoco<PublishedProviderIdsRequest>(publishedProviderIdsRequestJson);
            }

            await PerformPrerequisiteChecks(specificationId, 
                Job.Id, 
                batched == true ? PrerequisiteCheckerType.ApproveBatchProviders : PrerequisiteCheckerType.ApproveAllProviders);

            _logger.Information(logApproveProcessingMessage);

            _logger.Information("Fetching published providers for specification funding approval");
            
            IEnumerable<PublishedProvider> publishedProviders = await GetPublishedProvidersForApproval(specificationId, publishedProviderIdsRequest?.PublishedProviderIds?.ToArray());
            
            CheckPublishedProviderForErrors(specificationId, publishedProviders);

            Reference author = message.GetUserDetails();
            string correlationId = message.GetUserProperty<string>(SfaCorrelationId);

            await ApproveProviders(publishedProviders, 
                specificationId, 
                Job.Id, 
                author, 
                correlationId);
        }

        private async Task<IEnumerable<PublishedProvider>> GetPublishedProvidersForApproval(string specificationId, string[] publishedProviderIds = null)
        {
            _logger.Information("Fetching published providers for funding approval");

            Stopwatch existingPublishedProvidersStopwatch = Stopwatch.StartNew();
            IEnumerable<PublishedProvider> publishedProviders =
               await _publishedFundingDataService.GetPublishedProvidersForApproval(specificationId, publishedProviderIds);

            existingPublishedProvidersStopwatch.Stop();
            _logger.Information($"Fetched {publishedProviders.Count()} published providers for approval in {existingPublishedProvidersStopwatch.ElapsedMilliseconds} ms");

            return publishedProviders;
        }

        private async Task ApproveProviders(IEnumerable<PublishedProvider> publishedProviders, 
            string specificationId, 
            string jobId, 
            Reference author, 
            string correlationId)
        {
            string fundingPeriodId = publishedProviders.First().Current?.FundingPeriodId;

            using Transaction transaction = _transactionFactory.NewTransaction<ApproveService>();
            try
            {
                // if any error occurs while updating or indexing then we need to re-index all published providers for consistency
                transaction.Enroll(async () =>
                {
                    await _publishedProviderVersionService.CreateReIndexJob(author, correlationId, specificationId, jobId);
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
                    PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest = new PublishedFundingCsvJobsRequest
                    {
                        SpecificationId = specificationId,
                        CorrelationId = correlationId,
                        User = author,
                        FundingLineCodes = fundingLineCodes,
                        FundingStreamIds = fundingStreamIds,
                        FundingPeriodId = fundingPeriodId
                    };
                    await generateCsvJobs.CreateJobs(publishedFundingCsvJobsRequest);
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
                throw new NonRetriableException(
                    $"There are published providers with errors that must be fixed before they can be approved under specification {specificationId}.");
            }
        }

        private async Task PerformPrerequisiteChecks(string specificationId, string jobId, PrerequisiteCheckerType prerequisiteCheckerType)
        {
            _logger.Information($"Verifying prerequisites for {prerequisiteCheckerType}");

            IPrerequisiteChecker prerequisiteChecker = _prerequisiteCheckerLocator.GetPreReqChecker(prerequisiteCheckerType);
            try
            {
                await prerequisiteChecker.PerformChecks(specificationId, jobId, null, null);
            }
            catch (JobPrereqFailedException ex)
            {
                throw new NonRetriableException(ex.Message, ex);
            }

            _logger.Information($"Prerequisites for {prerequisiteCheckerType} passed");
        }
    }
}