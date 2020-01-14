using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderVersioningService : IPublishedProviderVersioningService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly IVersionRepository<PublishedProviderVersion> _versionRepository;
        private readonly Polly.Policy _versionRepositoryPolicy;
        private readonly IPublishingEngineOptions _publishingEngineOptions;

        public PublishedProviderVersioningService(
            ILogger logger,
            IVersionRepository<PublishedProviderVersion> versionRepository,
            IPublishingResiliencePolicies resiliencePolicies,
            IPublishingEngineOptions publishingEngineOptions)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(versionRepository, nameof(versionRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedProviderVersionRepository, nameof(resiliencePolicies.PublishedProviderVersionRepository));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));

            _logger = logger;
            _versionRepository = versionRepository;
            _publishingEngineOptions = publishingEngineOptions;
            _versionRepositoryPolicy = resiliencePolicies.PublishedProviderVersionRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth versioningRepo = await ((IHealthChecker)_versionRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderStatusUpdateService)
            };

            health.Dependencies.AddRange(versioningRepo.Dependencies);

            return health;
        }

        public IEnumerable<PublishedProviderCreateVersionRequest> AssemblePublishedProviderCreateVersionRequests(IEnumerable<PublishedProvider> publishedProviders, Reference author, PublishedProviderStatus publishedProviderStatus)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));
            Guard.ArgumentNotNull(author, nameof(author));

            IList<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests = new List<PublishedProviderCreateVersionRequest>();

            foreach (PublishedProvider publishedProvider in publishedProviders)
            {
                Guard.ArgumentNotNull(publishedProvider.Current, nameof(publishedProvider.Current));

                if (publishedProviderStatus != PublishedProviderStatus.Draft &&
                    (publishedProvider.Current.Status == publishedProviderStatus))
                {
                    continue;
                }

                PublishedProviderVersion newVersion = publishedProvider.Current.Clone() as PublishedProviderVersion;
                newVersion.Author = author;
                newVersion.Status = publishedProviderStatus;
                int minorVersion = publishedProvider.Current.MinorVersion;
                int majorVersion = publishedProvider.Current.MajorVersion;

                if ((publishedProvider.Current.Status == PublishedProviderStatus.Approved
                    || publishedProvider.Current.Status == PublishedProviderStatus.Released)
                    && publishedProviderStatus == PublishedProviderStatus.Updated)
                {
                    Interlocked.Increment(ref minorVersion);
                    newVersion.MinorVersion = minorVersion;
                }
                else if (publishedProviderStatus == PublishedProviderStatus.Released)
                {
                    Interlocked.Increment(ref majorVersion);
                    newVersion.MajorVersion = majorVersion;
                    newVersion.MinorVersion = 0;

                    publishedProvider.Released = newVersion;
                }

                switch (publishedProviderStatus)
                {
                    case PublishedProviderStatus.Approved:
                    case PublishedProviderStatus.Released:
                        newVersion.PublishStatus = Models.Versioning.PublishStatus.Approved;
                        break;

                    case PublishedProviderStatus.Updated:
                        newVersion.PublishStatus = Models.Versioning.PublishStatus.Updated;
                        break;

                    default:
                        newVersion.PublishStatus = Models.Versioning.PublishStatus.Draft;
                        break;
                }

                publishedProviderCreateVersionRequests.Add(new PublishedProviderCreateVersionRequest
                {
                    PublishedProvider = publishedProvider,
                    NewVersion = newVersion
                });
            }

            return publishedProviderCreateVersionRequests;
        }

        public async Task<IEnumerable<PublishedProvider>> CreateVersions(IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests)
        {
            Guard.ArgumentNotNull(publishedProviderCreateVersionRequests, nameof(publishedProviderCreateVersionRequests));

            ConcurrentBag<PublishedProvider> publishedProviders = new ConcurrentBag<PublishedProvider>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.PublishedProviderCreateVersionsConcurrencyCount);
            foreach (PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest in publishedProviderCreateVersionRequests)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            Guard.ArgumentNotNull(publishedProviderCreateVersionRequest.PublishedProvider, nameof(publishedProviderCreateVersionRequest.PublishedProvider));
                            Guard.ArgumentNotNull(publishedProviderCreateVersionRequest.NewVersion, nameof(publishedProviderCreateVersionRequest.NewVersion));

                            PublishedProviderVersion currentVersion = publishedProviderCreateVersionRequest.PublishedProvider.Current;

                            PublishedProviderVersion newVersion = publishedProviderCreateVersionRequest.NewVersion;

                            string partitionKey = currentVersion != null ? publishedProviderCreateVersionRequest.PublishedProvider.ParitionKey : string.Empty;

                            try
                            {
                                publishedProviderCreateVersionRequest.PublishedProvider.Current =
                                    await _versionRepositoryPolicy.ExecuteAsync(() => _versionRepository.CreateVersion(newVersion, currentVersion, partitionKey));
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, $"Failed to create new version for published provider version id: {newVersion.Id}");

                                throw;
                            }
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return publishedProviderCreateVersionRequests.Select(m => m.PublishedProvider);
        }

        public async Task SaveVersions(IEnumerable<PublishedProvider> publishedProviders)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));

            IEnumerable<KeyValuePair<string, PublishedProviderVersion>> versionsToSave = publishedProviders.Select(m =>
               new KeyValuePair<string, PublishedProviderVersion>(m.ParitionKey, m.Current));

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.PublishedProviderSaveVersionsConcurrencyCount);
            foreach (var versions in versionsToSave.ToBatches(10))
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            try
                            {
                                await _versionRepositoryPolicy.ExecuteAsync(() => _versionRepository.SaveVersions(versions));
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, "Failed to save new published provider versions");

                                throw;
                            }
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());


        }
    }
}
