using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderStatusUpdateService : IPublishedProviderStatusUpdateService, IHealthChecker
    {
        private readonly IPublishedProviderStatusUpdateSettings _settings;
        private readonly IJobTracker _jobTracker;
        private readonly IPublishedProviderVersioningService _publishedProviderVersioningService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly ILogger _logger;
        private readonly IPublishedFundingBulkRepository _publishedFundingBulkRepository;

        public PublishedProviderStatusUpdateService(
            IPublishedProviderVersioningService publishedProviderVersioningService,
            IPublishedFundingRepository publishedFundingRepository,
            IJobTracker jobTracker,
            ILogger logger,
            IPublishedProviderStatusUpdateSettings settings, 
            IPublishedFundingBulkRepository publishedFundingBulkRepository)
        {
            Guard.ArgumentNotNull(publishedProviderVersioningService, nameof(publishedProviderVersioningService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobTracker, nameof(jobTracker));
            Guard.ArgumentNotNull(settings, nameof(settings));
            Guard.ArgumentNotNull(publishedFundingBulkRepository, nameof(publishedFundingBulkRepository));

            _publishedProviderVersioningService = publishedProviderVersioningService;
            _publishedFundingRepository = publishedFundingRepository;
            _logger = logger;
            _settings = settings;
            _jobTracker = jobTracker;
            _publishedFundingBulkRepository = publishedFundingBulkRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth versioningService = await ((IHealthChecker)_publishedProviderVersioningService).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderStatusUpdateService)
            };

            health.Dependencies.AddRange(versioningService.Dependencies);

            return health;
        }

        public async Task<int> UpdatePublishedProviderStatus(IEnumerable<PublishedProvider> publishedProviders, Reference author,
            PublishedProviderStatus publishedProviderStatus, string jobId = null, string correlationId = null, bool force = false)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));
            Guard.ArgumentNotNull(author, nameof(author));

            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests = _publishedProviderVersioningService.AssemblePublishedProviderCreateVersionRequests(
                    publishedProviders.ToList(),
                    author, publishedProviderStatus, jobId, correlationId, force);


            if (!publishedProviderCreateVersionRequests.IsNullOrEmpty())
            {
                bool shouldNotifyProgress = !jobId.IsNullOrWhitespace();

                if (shouldNotifyProgress)
                {
                    await CreatePublishedProviderVersionsInBatches(publishedProviderStatus,
                        publishedProviderCreateVersionRequests.ToList(), jobId);
                }
                else
                {
                    await CreateLatestPublishedProviderVersions(publishedProviderStatus,
                        publishedProviderCreateVersionRequests);
                }

                return publishedProviderCreateVersionRequests.Count();
            }
            else
            {
                return 0;
            }
        }

        private async Task CreatePublishedProviderVersionsInBatches(PublishedProviderStatus publishedProviderStatus,
            List<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests,
            string jobId)
        {
            int batchSize = _settings.BatchSize;
            int currentCount = 0;
            int total = publishedProviderCreateVersionRequests.Count;

            while (currentCount < total)
            {
                IEnumerable<PublishedProviderCreateVersionRequest> batch = publishedProviderCreateVersionRequests
                    .Skip(currentCount)
                    .Take(batchSize);

                await CreateLatestPublishedProviderVersions(publishedProviderStatus, batch);

                currentCount += batchSize;

                await _jobTracker.NotifyProgress(Math.Min(currentCount, total), jobId);
            }
        }

        private async Task CreateLatestPublishedProviderVersions(PublishedProviderStatus publishedProviderStatus,
            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests)
        {
            IEnumerable<PublishedProvider> updatedPublishedProviders = await CreateVersions(publishedProviderCreateVersionRequests, publishedProviderStatus);

            if (!updatedPublishedProviders.Any())
            {
                return;
            }

            ConcurrentBag<HttpStatusCode> results = new ConcurrentBag<HttpStatusCode>();

            try
            {
                await _publishedFundingBulkRepository.UpsertPublishedProviders(
                    updatedPublishedProviders,
                    (Task<HttpStatusCode> task) =>
                    {
                        HttpStatusCode httpStatusCode = task.Result;
                        results.Add(httpStatusCode);
                    });

                if (results.Any(_ => !_.IsSuccess()))
                {
                    throw new Exception();
                }

                await SaveVersions(updatedPublishedProviders, publishedProviderStatus);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to create published Providers when updating status:' {publishedProviderStatus}' on published providers.";

                _logger.Error(ex, errorMessage);

                throw new RetriableException(errorMessage, ex);
            }
        }

        private async Task<IEnumerable<PublishedProvider>> CreateVersions(IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests,
            PublishedProviderStatus publishedProviderStatus = default)
        {
            IEnumerable<PublishedProvider> updatedPublishedProviders = null;

            try
            {
                updatedPublishedProviders = await _publishedProviderVersioningService.CreateVersions(publishedProviderCreateVersionRequests);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to create versions when updating status:' {publishedProviderStatus}' on published providers.";

                _logger.Error(ex, errorMessage);

                throw new RetriableException(errorMessage, ex);
            }

            return updatedPublishedProviders;
        }

        private async Task SaveVersions(IEnumerable<PublishedProvider> updatedPublishedProviders, PublishedProviderStatus publishedProviderStatus)
        {
            try
            {
                await _publishedProviderVersioningService.SaveVersions(updatedPublishedProviders);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to save versions when updating status:' {publishedProviderStatus}' on published providers.";

                _logger.Error(ex, errorMessage);

                throw new RetriableException(errorMessage, ex);
            }
        }


        private async Task DeleteVersions(IEnumerable<PublishedProvider> updatedPublishedProviders)
        {
            try
            {
                await _publishedProviderVersioningService.DeleteVersions(updatedPublishedProviders);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to delete versions on published providers.";

                _logger.Error(ex, errorMessage);

                throw new RetriableException(errorMessage, ex);
            }
        }
    }
}
