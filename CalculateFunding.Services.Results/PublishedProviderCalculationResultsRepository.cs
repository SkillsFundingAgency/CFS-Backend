using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results
{
    public class PublishedProviderCalculationResultsRepository : IPublishedProviderCalculationResultsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public PublishedProviderCalculationResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderCalculationResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public Task CreatePublishedCalculationResults(IEnumerable<PublishedProviderCalculationResult> publishedCalculationResults)
        {
            Guard.ArgumentNotNull(publishedCalculationResults, nameof(publishedCalculationResults));

            return _cosmosRepository.BulkCreateAsync(publishedCalculationResults.Select(m => new KeyValuePair<string, PublishedProviderCalculationResult>(m.ProviderId, m)), 100);
        }

        public Task<IEnumerable<PublishedProviderCalculationResultHistory>> GetPublishedProviderCalculationHistoryForSpecificationId(string specificationId)
        {
            IQueryable<PublishedProviderCalculationResultHistory> results = _cosmosRepository.Query<PublishedProviderCalculationResultHistory>(enableCrossPartitionQuery: true).Where(m => m.SpecificationId == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }

        public Task SavePublishedCalculationResultsHistory(IEnumerable<PublishedProviderCalculationResultHistory> publishedCalculationResultsHistory)
        {
            Guard.ArgumentNotNull(publishedCalculationResultsHistory, nameof(publishedCalculationResultsHistory));

            return _cosmosRepository.BulkCreateAsync<PublishedProviderCalculationResultHistory>(publishedCalculationResultsHistory.Select(m => new KeyValuePair<string, PublishedProviderCalculationResultHistory>(m.ProviderId, m)));
        }

        public Task<IEnumerable<PublishedProviderCalculationResult>> GetPublishedProviderCalculationResultsBySpecificationId(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            var results = _cosmosRepository.Query<PublishedProviderCalculationResult>(enableCrossPartitionQuery: true).Where(c => c.Specification.Id == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }

        public async Task<IEnumerable<PublishedProviderCalculationResult>> GetPublishedProviderCalculationResultsBySpecificationIdAndProviderId(string specificationId, IEnumerable<string> providerIds)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ConcurrentBag<PublishedProviderCalculationResult> results = new ConcurrentBag<PublishedProviderCalculationResult>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);
            foreach (string providerId in providerIds)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            string sql = $"select * from root c where c.content.specification.id = \"{specificationId}\" and c.documentType = \"PublishedProviderCalculationResult\" and c.content.providerId = \"{providerId}\" and c.deleted = false";

                            IEnumerable<PublishedProviderCalculationResult> publishedCalcResults = await _cosmosRepository.QueryPartitionedEntity<PublishedProviderCalculationResult>(sql, partitionEntityId: providerId);
                            foreach (PublishedProviderCalculationResult result in publishedCalcResults)
                            {
                                results.Add(result);
                            }
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return results;
        }

    }
}
