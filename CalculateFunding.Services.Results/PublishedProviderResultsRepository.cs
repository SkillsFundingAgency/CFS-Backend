using System;
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
using LinqKit;

namespace CalculateFunding.Services.Results
{
    public class PublishedProviderResultsRepository : IPublishedProviderResultsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public PublishedProviderResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public Task SavePublishedResults(IEnumerable<PublishedProviderResult> publishedResults)
        {
            Guard.ArgumentNotNull(publishedResults, nameof(publishedResults));

            return _cosmosRepository.BulkCreateAsync<PublishedProviderResult>(publishedResults.Select(m => new KeyValuePair<string, PublishedProviderResult>(m.ProviderId, m)), degreeOfParallelism: 25);
        }

        public Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsForSpecificationId(string specificationId)
        {
            IQueryable<PublishedProviderResult> results = _cosmosRepository.Query<PublishedProviderResult>(enableCrossPartitionQuery: true).Where(m => m.SpecificationId == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }

        public async Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsForSpecificationIdAndProviderId(string specificationId, IEnumerable<string> providerIds)
        {
            ConcurrentBag<PublishedProviderResult> results = new ConcurrentBag<PublishedProviderResult>();

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
                            string sql = $"select * from root c where c.content.specificationId = \"{specificationId}\" and c.documentType = \"PublishedProviderResult\" and c.content.providerId = \"{providerId}\" and c.deleted = false";

                            IEnumerable<PublishedProviderResult> publishedProviderResults = await _cosmosRepository.QueryPartitionedEntity<PublishedProviderResult>(sql, partitionEntityId: providerId);
                            foreach (PublishedProviderResult result in publishedProviderResults)
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

        public async Task<IEnumerable<PublishedProviderResultExisting>> GetExistingPublishedProviderResultsForSpecificationId(string specificationId)
        {
            IEnumerable<dynamic> existingResults = await _cosmosRepository.QueryDynamic<dynamic>($"SELECT r.id, r.content.providerId, r.content.fundingStreamResult.allocationLineResult.current.status,r.content.fundingStreamResult.allocationLineResult.current[\"value\"], r.content.fundingStreamResult.allocationLineResult.allocationLine.id as allocationLineId FROM Root r where r.documentType = 'PublishedProviderResult' and r.deleted = false and r.content.specificationId = '{specificationId}'", true, 1000);

            List<PublishedProviderResultExisting> results = new List<PublishedProviderResultExisting>();
            foreach (dynamic existingResult in existingResults)
            {
                PublishedProviderResultExisting result = new PublishedProviderResultExisting()
                {
                    AllocationLineId = existingResult.allocationLineId,
                    Id = existingResult.id,
                    ProviderId = existingResult.providerId,
                    Value = existingResult.value,
                };

                result.Status = Enum.Parse(typeof(AllocationLineStatus), existingResult.status);

                results.Add(result);
            }
            return results;
        }

        public Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(string fundingPeriod, string specificationId, string fundingStreamId)
        {
            IQueryable<PublishedProviderResult> results = _cosmosRepository.Query<PublishedProviderResult>(enableCrossPartitionQuery: true).Where(m => m.SpecificationId == specificationId && m.FundingPeriod.Id == fundingPeriod && m.FundingStreamResult.FundingStream.Id == fundingStreamId);

            return Task.FromResult(results.AsEnumerable());
        }

        public Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsForSpecificationAndStatus(string specificationId, UpdatePublishedAllocationLineResultStatusModel filterCriteria)
        {
            IQueryable<PublishedProviderResult> results = _cosmosRepository.Query<PublishedProviderResult>(enableCrossPartitionQuery: true).Where(m => m.SpecificationId == specificationId && m.FundingStreamResult.AllocationLineResult.Current.Status == filterCriteria.Status);

            var providerPredicate = PredicateBuilder.New<PublishedProviderResult>(false);

            foreach (var provider in filterCriteria.Providers)
            {
                string providerId = provider.ProviderId;
                providerPredicate = providerPredicate.Or(p => p.ProviderId == providerId);

                var allocationLinePredicate = PredicateBuilder.New<PublishedProviderResult>(false);
                foreach (var allocationLineId in provider.AllocationLineIds)
                {
                    string temp = allocationLineId;
                    allocationLinePredicate = allocationLinePredicate.Or(a => a.FundingStreamResult.AllocationLineResult.AllocationLine.Id == temp);
                }

                providerPredicate = providerPredicate.And(allocationLinePredicate);
            }

            results = results.AsExpandable().Where(providerPredicate);
            return Task.FromResult(results.AsEnumerable());
        }

        public PublishedProviderResult GetPublishedProviderResultForId(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));

            IQueryable<PublishedProviderResult> results = _cosmosRepository.Query<PublishedProviderResult>(enableCrossPartitionQuery: true).Where(m => m.Id == id);

            return results.AsEnumerable().FirstOrDefault();
        }

        public async Task<PublishedProviderResult> GetPublishedProviderResultForId(string publishedProviderResultId, string providerId)
        {
            Guard.IsNullOrWhiteSpace(publishedProviderResultId, nameof(publishedProviderResultId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            IEnumerable<PublishedProviderResult> results = await _cosmosRepository.QueryPartitionedEntity<PublishedProviderResult>($"SELECT * FROM Root r WHERE r.id ='{publishedProviderResultId}'", 1, providerId);

            return results.FirstOrDefault();
        }

        public Task<PublishedProviderResult> GetPublishedProviderResultForIdInPublishedState(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));

            IQueryable<PublishedProviderResult> results = _cosmosRepository.Query<PublishedProviderResult>(enableCrossPartitionQuery: true).Where(m => m.Id == id &&
                m.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Published);

            return Task.FromResult(results.AsEnumerable().FirstOrDefault());
        }

        public Task<PublishedAllocationLineResultHistory> GetPublishedAllocationLineResultHistoryForId(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));

            IQueryable<PublishedAllocationLineResultHistory> results = _cosmosRepository.Query<PublishedAllocationLineResultHistory>(enableCrossPartitionQuery: true).Where(m => m.AllocationResultId == id);

            return Task.FromResult(results.AsEnumerable().FirstOrDefault());
        }

        public Task<IEnumerable<PublishedAllocationLineResultHistory>> GetPublishedProviderAllocationLineHistoryForSpecificationId(string specificationId)
        {
            IQueryable<PublishedAllocationLineResultHistory> results = _cosmosRepository.Query<PublishedAllocationLineResultHistory>(enableCrossPartitionQuery: true).Where(m => m.SpecificationId == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }

        public Task SavePublishedAllocationLineResultsHistory(IEnumerable<PublishedAllocationLineResultHistory> publishedResultsHistory)
        {
            Guard.ArgumentNotNull(publishedResultsHistory, nameof(publishedResultsHistory));

            return _cosmosRepository.BulkCreateAsync<PublishedAllocationLineResultHistory>(publishedResultsHistory.Select(m => new KeyValuePair<string, PublishedAllocationLineResultHistory>(m.ProviderId, m)));
        }

        public async Task<PublishedAllocationLineResultHistory> GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(string specificationId, string providerId, string allocationLineId)
        {
            string query = $"SELECT * from r where r.content.providerId = '{providerId}' and r.content.specificationId = '{specificationId}' and r.content.allocationLine.id = '{allocationLineId}' and r.documentType = 'PublishedAllocationLineResultHistory'";

            return (await _cosmosRepository.QueryPartitionedEntity<PublishedAllocationLineResultHistory>(query, 1, providerId)).FirstOrDefault();
        }

        public async Task<IEnumerable<PublishedProviderResult>> GetAllNonHeldPublishedProviderResults()
        {
            IEnumerable<DocumentEntity<PublishedProviderResult>> documentEntities = await _cosmosRepository.GetAllDocumentsAsync<PublishedProviderResult>(
                query: m => m.Content.FundingStreamResult.AllocationLineResult.Current.Status != AllocationLineStatus.Held, enableCrossPartitionQuery: true);

            return documentEntities.Select(m => m.Content);
        }
    }
}
