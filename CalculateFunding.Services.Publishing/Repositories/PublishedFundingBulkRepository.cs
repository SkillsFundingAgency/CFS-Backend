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

        public PublishedFundingBulkRepository(
            IPublishingResiliencePolicies resiliencePolicies,
            ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));

            _repository = cosmosRepository;
            _publishedFundingRepositoryPolicy = resiliencePolicies.PublishedFundingRepository;
        }

        public async Task<IEnumerable<PublishedFunding>> GetPublishedFundings(
            IEnumerable<KeyValuePair<string, string>> publishedFundingIds)
        {
            List<Task<DocumentEntity<PublishedFunding>>> concurrentTasks = new List<Task<DocumentEntity<PublishedFunding>>>();

            foreach (KeyValuePair<string, string> publishedFundingIdPair in publishedFundingIds)
            {
                concurrentTasks.Add(
                    _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                        _repository.ReadDocumentByIdPartitionedAsync<PublishedFunding>(
                            publishedFundingIdPair.Key,
                            publishedFundingIdPair.Value)));
            }

            await Task.WhenAll(concurrentTasks);

            return concurrentTasks.Select(_ => _.Result.Content);
        }

        public async Task<IEnumerable<PublishedFundingVersion>> GetPublishedFundingVersions(
            IEnumerable<KeyValuePair<string, string>> publishedFundingVersionIds)
        {
            List<Task<DocumentEntity<PublishedFundingVersion>>> concurrentTasks = new List<Task<DocumentEntity<PublishedFundingVersion>>>();

            foreach (KeyValuePair<string, string> publishedFundingVersionIdPair in publishedFundingVersionIds)
            {
                concurrentTasks.Add(
                    _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                        _repository.ReadDocumentByIdPartitionedAsync<PublishedFundingVersion>(
                            publishedFundingVersionIdPair.Key,
                            publishedFundingVersionIdPair.Value)));
            }

            await Task.WhenAll(concurrentTasks);

            return concurrentTasks.Select(_ => _.Result.Content);
        }

        public async Task<IEnumerable<PublishedProvider>> GetPublishedProviders(
            IEnumerable<KeyValuePair<string, string>> publishedProviderIds)
        {
            List<Task<DocumentEntity<PublishedProvider>>> concurrentTasks = new List<Task<DocumentEntity<PublishedProvider>>>();

            foreach (KeyValuePair<string, string> publishedFundingIdPair in publishedProviderIds)
            {
                concurrentTasks.Add(
                    _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                        _repository.ReadDocumentByIdPartitionedAsync<PublishedProvider>(
                            publishedFundingIdPair.Key,
                            publishedFundingIdPair.Value)));
            }

            await Task.WhenAll(concurrentTasks);

            return concurrentTasks.Select(_ => _.Result.Content);
        }

        public async Task UpsertPublishedFundings(
            IEnumerable<PublishedFunding> publishedFundings,
            Action<Task<HttpStatusCode>, PublishedFunding> continueAction)
        {
            List<Task> concurrentTasks = new List<Task>();

            foreach (PublishedFunding publishedFunding in publishedFundings)
            {
                concurrentTasks.Add(
                    _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                    _repository.UpsertAsync(publishedFunding, publishedFunding.ParitionKey)
                        .ContinueWith((task) => continueAction(task, publishedFunding))
                    ));
            }

            await Task.WhenAll(concurrentTasks);
        }

        public async Task UpsertPublishedProviders(
            IEnumerable<PublishedProvider> publishedProviders,
            Action<Task<HttpStatusCode>> continueAction)
        {
            List<Task> concurrentTasks = new List<Task>();

            foreach (PublishedProvider publishedProvider in publishedProviders)
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
