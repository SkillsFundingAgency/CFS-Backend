using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Migration;

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
            (bool Ok,string Message) cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderCalculationResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public Task<IEnumerable<Migration.PublishedProviderCalculationResult>> GetPublishedProviderCalculationResultsBySpecificationId(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IQueryable<Migration.PublishedProviderCalculationResult> results = _cosmosRepository.Query<Migration.PublishedProviderCalculationResult>(enableCrossPartitionQuery: true).Where(c => c.Specification.Id == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }

        public async Task<IEnumerable<Migration.PublishedProviderCalculationResult>> GetFundingOrPublicPublishedProviderCalculationResultsBySpecificationIdAndProviderId(string specificationId, IEnumerable<string> providerIds)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ConcurrentBag<Migration.PublishedProviderCalculationResult> results = new ConcurrentBag<Migration.PublishedProviderCalculationResult>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 30);
            foreach (string providerId in providerIds)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            string sql = $"select * from root c where c.content.specification.id = \"{specificationId}\" and c.documentType = \"PublishedProviderCalculationResult\" and (c.content.current.calculationType = 'Funding' or c.content.isPublic = true or c.content.current.calculationType = 'Baseline') and c.content.providerId = \"{providerId}\" and c.deleted = false";

                            IEnumerable<Migration.PublishedProviderCalculationResult> publishedCalcResults = await _cosmosRepository.QueryPartitionedEntity<Migration.PublishedProviderCalculationResult>(sql, partitionEntityId: providerId);
                            foreach (Migration.PublishedProviderCalculationResult result in publishedCalcResults)
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

        public async Task<Migration.PublishedProviderCalculationResult> GetPublishedProviderCalculationResultForId(string publishedProviderCalculationResultId, string providerId)
        {
            Guard.IsNullOrWhiteSpace(publishedProviderCalculationResultId, nameof(publishedProviderCalculationResultId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            IEnumerable<Migration.PublishedProviderCalculationResult> results = await _cosmosRepository.QueryPartitionedEntity<Migration.PublishedProviderCalculationResult>($"SELECT * FROM Root r WHERE r.id ='{publishedProviderCalculationResultId}'", 1, providerId);

            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<PublishedProviderCalculationResultVersion>> GetPublishedCalculationVersions(string specificationId, string providerId)
        {
            string sql = $"select * from root c where c.content.specification.id = \"{specificationId}\" and c.documentType = \"PublishedProviderCalculationResultVersion\" c.content.providerId = \"{providerId}\" and c.deleted = false";

            IEnumerable<PublishedProviderCalculationResultVersion> publishedCalcResults = await _cosmosRepository.QueryPartitionedEntity<PublishedProviderCalculationResultVersion>(sql, partitionEntityId: providerId);

            return publishedCalcResults;
        }

        public Task CreatePublishedCalculationResults(IEnumerable<Migration.PublishedProviderCalculationResult> publishedCalculationResults)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<PublishedProviderCalculationResultExisting>> GetExistingPublishedProviderCalculationResultsForSpecificationId(string specificationId)
        {
            throw new System.NotImplementedException();
        }
    }
}