using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Repositories
{
    public class PublishedFundingBulkRepository : IPublishedFundingBulkRepository
    {
        private readonly Polly.AsyncPolicy _publishedFundingRepositoryPolicy;

        private readonly ICosmosRepository _repository;

        private readonly int _batchSize;

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
            _batchSize = publishingEngineOptions.MaxBatchSizePublishedFunding;
            _publishedFundingRepositoryPolicy = resiliencePolicies.PublishedFundingRepository;
        }

        public async Task<IEnumerable<PublishedFunding>> GetPublishedFundings(
            IEnumerable<KeyValuePair<string, string>> publishedFundingIds)
        {
            List<PublishedFunding> concurrentObjects = new List<PublishedFunding>();

            foreach (IEnumerable<KeyValuePair<string, string>> batchPublishedFundingIds in publishedFundingIds.ToBatches(_batchSize))
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

                await Task.WhenAll(concurrentTasks);

                concurrentObjects.AddRange(concurrentTasks.Select(_ => _.Result.Content));
            }

            return concurrentObjects;
        }

        public async Task<IEnumerable<PublishedFundingVersion>> GetPublishedFundingVersions(
            IEnumerable<KeyValuePair<string, string>> publishedFundingVersionIds)
        {
            List<PublishedFundingVersion> concurrentObjects = new List<PublishedFundingVersion>();

            foreach (IEnumerable<KeyValuePair<string, string>> batchPublishedFundingVersionIds in publishedFundingVersionIds.ToBatches(_batchSize))
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

                await Task.WhenAll(concurrentTasks);

                concurrentObjects.AddRange(concurrentTasks.Select(_ => _.Result.Content));
            }

            return concurrentObjects;
        }

        public async Task<IEnumerable<PublishedProvider>> GetPublishedProviders(
            IEnumerable<KeyValuePair<string, string>> publishedProviderIds)
        {
            List<PublishedProvider> concurrentObjects = new List<PublishedProvider>();

            foreach (IEnumerable<KeyValuePair<string, string>> batchPublishedProviderIds in publishedProviderIds.ToBatches(_batchSize))
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

                await Task.WhenAll(concurrentTasks);

                concurrentObjects.AddRange(concurrentTasks.Select(_ => _.Result.Content));
            }

            return concurrentObjects;
        }

        public async Task UpsertPublishedFundings(
            IEnumerable<PublishedFunding> publishedFundings,
            Action<Task<HttpStatusCode>, PublishedFunding> continueAction)
        {
            foreach (IEnumerable<IEnumerable<PublishedFunding>> batchPublishedFundings in publishedFundings.ToBatches(_batchSize))
            {
                List<Task> concurrentTasks = new List<Task>();

                foreach (PublishedFunding publishedFunding in batchPublishedFundings)
                {
                    concurrentTasks.Add(
                        _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                        _repository.UpsertAsync(publishedFunding, publishedFunding.ParitionKey)
                            .ContinueWith((task) => continueAction(task, publishedFunding))
                        ));
                }

                await Task.WhenAll(concurrentTasks);
            }
        }

        public async Task UpsertPublishedProviders(
            IEnumerable<PublishedProvider> publishedProviders,
            Action<Task<HttpStatusCode>> continueAction)
        {
            foreach (IEnumerable<IEnumerable<PublishedProvider>> batchPublishedProviders in publishedProviders.ToBatches(_batchSize))
            {
                List<Task> concurrentTasks = new List<Task>();

                foreach (PublishedProvider publishedProvider in batchPublishedProviders)
                {
                    concurrentTasks.Add(
                        _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                            _repository.UpsertAsync(publishedProvider, publishedProvider.PartitionKey)
                                .ContinueWith((task) => continueAction(task))
                        ));
                }

                await Task.WhenAll(concurrentTasks);
            }
        }
    }
}
