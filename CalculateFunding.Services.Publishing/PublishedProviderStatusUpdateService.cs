using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
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
        private readonly IPublishingEngineOptions _publishingEngineOptions;

        public PublishedProviderStatusUpdateService(IPublishedProviderVersioningService publishedProviderVersioningService,
            IPublishedFundingRepository publishedFundingRepository,
            IJobTracker jobTracker,
            ILogger logger,
            IPublishedProviderStatusUpdateSettings settings, 
            IPublishingEngineOptions publishingEngineOptions)
        {
            Guard.ArgumentNotNull(publishedProviderVersioningService, nameof(publishedProviderVersioningService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobTracker, nameof(jobTracker));
            Guard.ArgumentNotNull(settings, nameof(settings));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));

            _publishedProviderVersioningService = publishedProviderVersioningService;
            _publishedFundingRepository = publishedFundingRepository;
            _logger = logger;
            _settings = settings;
            _publishingEngineOptions = publishingEngineOptions;
            _jobTracker = jobTracker;
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

        public async Task UpdatePublishedProviderStatus(IEnumerable<PublishedProvider> publishedProviders,
            Reference author,
            PublishedProviderStatus publishedProviderStatus,
            string jobId = null)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));
            Guard.ArgumentNotNull(author, nameof(author));

            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests =
               _publishedProviderVersioningService.AssemblePublishedProviderCreateVersionRequests(publishedProviders.ToList(), author, publishedProviderStatus);

            if (publishedProviderCreateVersionRequests.IsNullOrEmpty())
            {
                string errorMessage = "No published providers were assembled for updating.";

                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }

            bool shouldNotifyProgress = !jobId.IsNullOrWhitespace();

            if (shouldNotifyProgress)
            {
                await CreatePublishedProviderVersionsInBatches(publishedProviderStatus, publishedProviderCreateVersionRequests.ToList(), jobId);
            }
            else
            {
                await CreateLatestPublishedProviderVersions(publishedProviderStatus, publishedProviderCreateVersionRequests);
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

            if (updatedPublishedProviders.Any())
            {
                string errorMessage = $"Failed to create published Providers when updating status:' {publishedProviderStatus}' on published providers.";

                ConcurrentBag<HttpStatusCode> results = new ConcurrentBag<HttpStatusCode>();

                try
                {
                    List<Task> allTasks = new List<Task>();
                    SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.CreateLatestPublishedProviderVersionsConcurrencyCount);
                    foreach (PublishedProvider publishedProvider in updatedPublishedProviders)
                    {
                        await throttler.WaitAsync();
                        allTasks.Add(
                            Task.Run(async () =>
                            {
                                try
                                {
                                    IEnumerable<PublishedProvider> publishedProviders = new[] { publishedProvider };
                                    IEnumerable<HttpStatusCode> result = await _publishedFundingRepository.UpsertPublishedProviders(publishedProviders);
                                    foreach (HttpStatusCode httpStatusCode in result)
                                    {
                                        results.Add(httpStatusCode);
                                    }
                                }
                                finally
                                {
                                    throttler.Release();
                                }
                            }));
                    }
                    await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

                    if (results.All(_ => _.IsSuccess()))
                    {
                        await SaveVersions(updatedPublishedProviders, publishedProviderStatus);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, errorMessage);

                    throw new RetriableException(errorMessage, ex);
                }
            }
        }

        private async Task<IEnumerable<PublishedProvider>> CreateVersions(IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests,
            PublishedProviderStatus publishedProviderStatus)
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
    }
}
