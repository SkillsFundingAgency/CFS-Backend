using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class ApproveService : IApproveService
    {
        private readonly IJobTracker _jobTracker;
        private readonly ILogger _logger;
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly IPublishedProviderIndexerService _publishedProviderIndexerService;

        public ApproveService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishedProviderIndexerService publishedProviderIndexerService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IJobTracker jobTracker,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobTracker, nameof(jobTracker));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishedProviderIndexerService, nameof(publishedProviderIndexerService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingDataService = publishedFundingDataService;
            _publishedProviderIndexerService = publishedProviderIndexerService;
            _jobTracker = jobTracker;
            _logger = logger;
        }

        public async Task ApproveResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            _logger.Information("Starting approve funding job");
            string jobId = message.GetUserProperty<string>("jobId");

            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            if (!await _jobTracker.TryStartTrackingJob(jobId, "ApproveResults"))
            {
                return;
            }

            Reference author = message.GetUserDetails();

            string specificationId = message.GetUserProperty<string>("specification-id");
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
            await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved, jobId);

            _logger.Information($"Indexing published providers");
            await _publishedProviderIndexerService.IndexPublishedProviders(publishedProviders.Select(_ => _.Current));

            _logger.Information($"Completing approve funding job. JobId='{jobId}'");
            await _jobTracker.CompleteTrackingJob(jobId);
            _logger.Information($"Approve funding job complete. JobId='{jobId}'");
        }
    }
}