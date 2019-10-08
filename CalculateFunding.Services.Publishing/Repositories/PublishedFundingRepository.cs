using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Documents;

namespace CalculateFunding.Services.Publishing.Repositories
{
    public class PublishedFundingRepository : IPublishedFundingRepository
    {
        private readonly ICosmosRepository _repository;

        public PublishedFundingRepository(ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _repository = cosmosRepository;
        }

        public async Task<IEnumerable<HttpStatusCode>> UpsertPublishedProviders(IEnumerable<PublishedProvider> publishedProviders)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));

            IEnumerable<Task<HttpStatusCode>> tasks = publishedProviders.Select(async (_) => await _repository.UpsertAsync(_, _.ParitionKey));

            await TaskHelper.WhenAllAndThrow(tasks.ToArray());

            return tasks.Select(_ => _.Result);
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) cosmosRepoHealth = await _repository.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(PublishedFundingRepository)
            };

            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = cosmosRepoHealth.Ok,
                DependencyName = _repository.GetType().GetFriendlyName(),
                Message = cosmosRepoHealth.Message
            });

            return health;
        }

        public async Task<PublishedProviderVersion> GetPublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            string version)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));
            Guard.IsNullOrWhiteSpace(version, nameof(version));


            string id = $"publishedprovider-{fundingStreamId}-{fundingPeriodId}-{providerId}-{version}";

            return (await _repository.ReadAsync<PublishedProviderVersion>(id, true))?.Content;
        }

        public async Task<HttpStatusCode> UpsertPublishedFunding(PublishedFunding publishedFunding)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));

            return await _repository.UpsertAsync(publishedFunding, publishedFunding.ParitionKey);
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIdsForApproval(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));


            Dictionary<string, string> results = new Dictionary<string, string>();
            IEnumerable<dynamic> queryResults = _repository
             .DynamicQuery<dynamic>(new SqlQuerySpec
             {
                 QueryText = @"
                                SELECT c.id as id, c.content.partitionKey as partitionKey FROM c
                                WHERE c.documentType = 'PublishedProvider'
                                AND c.content.current.fundingStreamId = @fundingStreamId
                                AND c.content.current.fundingPeriodId = @fundingPeriodId
                                AND (c.content.current.status = 'Draft' OR c.content.current.status = 'Updated')",
                 Parameters = new SqlParameterCollection
                 {
                                    new SqlParameter("@fundingStreamId", fundingStreamId),
                                    new SqlParameter("@fundingPeriodId", fundingPeriodId)
                 }
             }, true);

            foreach (dynamic item in queryResults)
            {
                results.Add(item.id, item.partitionKey);
            }

            return await Task.FromResult(results);
        }

        public async Task<PublishedProvider> GetPublishedProviderById(string cosmosId, string partitionKey)
        {
            Guard.IsNullOrWhiteSpace(cosmosId, nameof(cosmosId));
            Guard.IsNullOrWhiteSpace(partitionKey, nameof(partitionKey));

            return await _repository.ReadByIdPartitionedAsync<PublishedProvider>(cosmosId, partitionKey);
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIds(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));


            Dictionary<string, string> results = new Dictionary<string, string>();
            IEnumerable<dynamic> queryResults = _repository
             .DynamicQuery<dynamic>(new SqlQuerySpec
             {
                 QueryText = @"
                                SELECT c.id as id, c.content.partitionKey as partitionKey FROM c
                                WHERE c.documentType = 'PublishedProvider'
                                AND c.content.current.fundingStreamId = @fundingStreamId
                                AND c.content.current.fundingPeriodId = @fundingPeriodId",
                 Parameters = new SqlParameterCollection
                 {
                                    new SqlParameter("@fundingStreamId", fundingStreamId),
                                    new SqlParameter("@fundingPeriodId", fundingPeriodId)
                 }
             }, true);

            foreach (dynamic item in queryResults)
            {
                results.Add(item.id, item.partitionKey);
            }

            return await Task.FromResult(results);
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingIds(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));


            Dictionary<string, string> results = new Dictionary<string, string>();
            IEnumerable<dynamic> queryResults = _repository
             .DynamicQuery<dynamic>(new SqlQuerySpec
             {
                 QueryText = @"
                                SELECT c.id as id, c.content.partitionKey as partitionKey FROM c
                                WHERE c.documentType = 'PublishedFunding'
                                AND c.content.current.fundingStreamId = @fundingStreamId
                                AND c.content.current.fundingPeriod.id = @fundingPeriodId",
                 Parameters = new SqlParameterCollection
                 {
                                    new SqlParameter("@fundingStreamId", fundingStreamId),
                                    new SqlParameter("@fundingPeriodId", fundingPeriodId)
                 }
             }, true);

            foreach (dynamic item in queryResults)
            {
                results.Add(item.id, item.partitionKey);
            }

            return await Task.FromResult(results);
        }

        public async Task<PublishedFunding> GetPublishedFundingById(string cosmosId, string partitionKey)
        {
            Guard.IsNullOrWhiteSpace(cosmosId, nameof(cosmosId));
            Guard.IsNullOrWhiteSpace(partitionKey, nameof(partitionKey));

            return await _repository.ReadByIdPartitionedAsync<PublishedFunding>(cosmosId, partitionKey);
        }
    }
}