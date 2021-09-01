using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Repositories
{
    public class PublishedFundingBulkRepository : IPublishedFundingBulkRepository
    {
        private readonly Polly.AsyncPolicy _publishedFundingRepositoryPolicy;

        private readonly ICosmosRepository _repository;

        private readonly IPublishingEngineOptions _publishingEngineOptions;

        public PublishedFundingBulkRepository(
            IPublishingResiliencePolicies resiliencePolicies,
            IPublishingEngineOptions publishingEngineOptions,
            ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));

            _repository = cosmosRepository;
            _publishingEngineOptions = publishingEngineOptions;
            _publishedFundingRepositoryPolicy = resiliencePolicies.PublishedFundingRepository;
        }

        public async Task<IEnumerable<PublishedFunding>> GetPublishedFundings(
            IEnumerable<KeyValuePair<string, string>> publishedFundingIds)
        {
            ConcurrentBag<PublishedFunding> concurrentObjects = new ConcurrentBag<PublishedFunding>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.PublishedFundingConcurrencyCount);
            foreach (IEnumerable<KeyValuePair<string, string>> batchPublishedFundingIds in publishedFundingIds.ToBatches(_publishingEngineOptions.MaxBatchSizePublishedFunding))
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            List<Task<DocumentEntity<PublishedFunding>>> concurrentTasks = new List<Task<DocumentEntity<PublishedFunding>>>();

                            foreach (KeyValuePair<string, string> publishedFundingIdPair in batchPublishedFundingIds)
                            {
                                concurrentTasks.Add(
                                    _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                                        _repository.ReadDocumentByIdPartitionedAsync<PublishedFunding>(
                                            publishedFundingIdPair.Key,
                                            publishedFundingIdPair.Value)));
                            }

                            await TaskHelper.WhenAllAndThrow(concurrentTasks.ToArraySafe());

                            concurrentTasks.ForEach(_ => concurrentObjects.Add(_.Result.Content));
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return concurrentObjects;
        }

        public async Task<IEnumerable<PublishedFundingVersion>> GetPublishedFundingVersions(
            IEnumerable<KeyValuePair<string, string>> publishedFundingVersionIds)
        {
            ConcurrentBag<PublishedFundingVersion> concurrentObjects = new ConcurrentBag<PublishedFundingVersion>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.PublishedFundingConcurrencyCount);
            foreach (IEnumerable<KeyValuePair<string, string>> batchPublishedFundingVersionIds in publishedFundingVersionIds.ToBatches(_publishingEngineOptions.MaxBatchSizePublishedFunding))
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            List<Task<DocumentEntity<PublishedFundingVersion>>> concurrentTasks = new List<Task<DocumentEntity<PublishedFundingVersion>>>();

                            foreach (KeyValuePair<string, string> publishedFundingVersionIdPair in batchPublishedFundingVersionIds)
                            {
                                concurrentTasks.Add(
                                    _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                                        _repository.ReadDocumentByIdPartitionedAsync<PublishedFundingVersion>(
                                            publishedFundingVersionIdPair.Key,
                                            publishedFundingVersionIdPair.Value)));
                            }

                            await TaskHelper.WhenAllAndThrow(concurrentTasks.ToArraySafe());

                            concurrentTasks.ForEach(_ => concurrentObjects.Add(_.Result.Content));
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return concurrentObjects;
        }

        public async Task<IEnumerable<PublishedProvider>> GetPublishedProviders(
            IEnumerable<KeyValuePair<string, string>> publishedProviderIds)
        {
            ConcurrentBag<PublishedProvider> concurrentObjects = new ConcurrentBag<PublishedProvider>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.PublishedFundingConcurrencyCount);
            foreach (IEnumerable<KeyValuePair<string, string>> batchPublishedProviderIds in publishedProviderIds.ToBatches(_publishingEngineOptions.MaxBatchSizePublishedFunding))
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            List<Task<DocumentEntity<PublishedProvider>>> concurrentTasks = new List<Task<DocumentEntity<PublishedProvider>>>();

                            foreach (KeyValuePair<string, string> publishedFundingIdPair in batchPublishedProviderIds)
                            {
                                concurrentTasks.Add(
                                    _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                                        _repository.ReadDocumentByIdPartitionedAsync<PublishedProvider>(
                                            publishedFundingIdPair.Key,
                                            publishedFundingIdPair.Value)));
                            }

                            await TaskHelper.WhenAllAndThrow(concurrentTasks.ToArraySafe());

                            concurrentTasks.ForEach(_ => concurrentObjects.Add(_.Result.Content));
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return concurrentObjects;
        }

        public async Task<IEnumerable<PublishedProvider>> TryGetPublishedProvidersByProviderId(
            IEnumerable<string> providerIds, string fundingStreamId, string fundingPeriodId)
        {
            ConcurrentBag<PublishedProvider> concurrentObjects = new ConcurrentBag<PublishedProvider>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.PublishedFundingConcurrencyCount);

            foreach (string providerId in providerIds)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            string cosmosDocumentId = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}";

                            DocumentEntity<PublishedProvider> publishedProvider = await _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                                 _repository.ReadDocumentByIdPartitionedAsync<PublishedProvider>(
                                     cosmosDocumentId,
                                     cosmosDocumentId));

                            if (publishedProvider != null && !publishedProvider.Deleted)
                            {
                                concurrentObjects.Add(publishedProvider.Content);
                            }

                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }


            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return concurrentObjects;
        }

        public async Task UpsertPublishedFundings(
            IEnumerable<PublishedFunding> publishedFundings,
            Action<Task<HttpStatusCode>, PublishedFunding> continueAction)
        {
            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.PublishedFundingConcurrencyCount);
            foreach (IEnumerable<PublishedFunding> batchPublishedFundings in publishedFundings.ToBatches(_publishingEngineOptions.MaxBatchSizePublishedFunding))
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            List<Task> concurrentTasks = new List<Task>();

                            foreach (PublishedFunding publishedFunding in batchPublishedFundings)
                            {
                                concurrentTasks.Add(
                                    _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                                    _repository.UpsertAsync(publishedFunding, publishedFunding.ParitionKey, undelete: true)
                                        .ContinueWith((task) => continueAction(task, publishedFunding))
                                    ));
                            }

                            await TaskHelper.WhenAllAndThrow(concurrentTasks.ToArraySafe());
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());
        }

        public async Task UpsertPublishedProviders(
            IEnumerable<PublishedProvider> publishedProviders,
            Action<Task<HttpStatusCode>> continueAction)
        {
            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.PublishedFundingConcurrencyCount);
            foreach (IEnumerable<PublishedProvider> batchPublishedProviders in publishedProviders.ToBatches(_publishingEngineOptions.MaxBatchSizePublishedFunding))
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            List<Task> concurrentTasks = new List<Task>();

                            foreach (PublishedProvider publishedProvider in batchPublishedProviders)
                            {
                                concurrentTasks.Add(
                                    _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                                        _repository.UpsertAsync(publishedProvider, publishedProvider.PartitionKey, undelete: true)
                                            .ContinueWith((task) => continueAction(task))
                                    ));
                            }

                            await TaskHelper.WhenAllAndThrow(concurrentTasks.ToArraySafe());
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
