using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

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
            (bool Ok, string Message) cosmosRepoHealth = _repository.IsHealthOk();

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

        public async Task<IEnumerable<dynamic>> GetFundings(string publishedProviderVersion)
        {
            Guard.IsNullOrWhiteSpace(publishedProviderVersion, nameof(publishedProviderVersion));

            return await _repository
             .DynamicQuery(new CosmosDbQuery
             {
                 QueryText = @"SELECT c.content.fundingId As fundingId FROM c
                                where c.documentType = 'PublishedFundingVersion'
                                AND ARRAY_CONTAINS(c.content.providerFundings, @publishedProviderVersion, true)
                                and c.content.status = 'Released'
                                and c.deleted = false",
                 Parameters = new[]
                 {
                    new CosmosDbQueryParameter("@publishedProviderVersion", publishedProviderVersion)
                 }
             });
        }

        public async Task<PublishedProviderVersion> GetLatestPublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            return (await _repository
                .QuerySql<PublishedProvider>(new CosmosDbQuery
                {
                    QueryText =@"SELECT *
                                 FROM c
                                 WHERE c.documentType = 'PublishedProvider'
                                 AND c.deleted = false
                                 AND c.content.current.providerId = @providerId
                                 AND c.content.current.fundingStreamId = @fundingStreamId
                                 AND c.content.current.fundingPeriodId = @fundingPeriodId",
                    Parameters = new[]
                    {
                        new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                        new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId),
                        new CosmosDbQueryParameter("@providerId", providerId)
                    }
                }))
                .SingleOrDefault()
                ?.Current;
        }
        
        public async Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersions(string specificationId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            return await _repository
             .QuerySql<PublishedProviderVersion>(new CosmosDbQuery
             {
                 QueryText = @"SELECT * FROM c
                                WHERE c.content.specificationId = @specificationId
                                AND c.content.providerId = @providerId
                                AND c.deleted = false
                                AND c.documentType = 'PublishedProviderVersion'
                                ORDER BY c.content.date desc",
                 Parameters = new[]
                 {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                    new CosmosDbQueryParameter("@providerId", providerId)
                 }
             });
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

            return (await _repository.ReadDocumentByIdPartitionedAsync<PublishedProviderVersion>(id, id))?.Content;
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
            IEnumerable<dynamic> queryResults = await _repository
             .DynamicQuery(new CosmosDbQuery
             {
                 QueryText = @"
                                SELECT c.id as id, c.content.partitionKey as partitionKey FROM c
                                WHERE c.documentType = 'PublishedProvider'
                                AND c.content.current.fundingStreamId = @fundingStreamId
                                AND c.content.current.fundingPeriodId = @fundingPeriodId
                                AND (c.content.current.status = 'Draft' OR c.content.current.status = 'Updated')",
                 Parameters = new[]
                 {
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                 }
             });

            foreach (dynamic item in queryResults)
            {
                results.Add((string)item.id, (string)item.partitionKey);
            }

            return await Task.FromResult(results);
        }

        public async Task<PublishedProvider> GetPublishedProviderById(string cosmosId, string partitionKey)
        {
            Guard.IsNullOrWhiteSpace(cosmosId, nameof(cosmosId));
            Guard.IsNullOrWhiteSpace(partitionKey, nameof(partitionKey));

            return (await _repository.ReadDocumentByIdPartitionedAsync<PublishedProvider>(cosmosId, partitionKey))?.Content;
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIds(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));


            Dictionary<string, string> results = new Dictionary<string, string>();
            IEnumerable<dynamic> queryResults = await _repository
             .DynamicQuery(new CosmosDbQuery
             {
                 QueryText = @"
                                SELECT c.id as id, c.content.partitionKey as partitionKey FROM c
                                WHERE c.documentType = 'PublishedProvider'
                                AND c.content.current.fundingStreamId = @fundingStreamId
                                AND c.content.current.fundingPeriodId = @fundingPeriodId",
                 Parameters = new[]
                 {
                                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                 }
             });

            foreach (dynamic item in queryResults)
            {
                results.Add((string)item.id, (string)item.partitionKey);
            }

            return await Task.FromResult(results);
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingIds(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));


            Dictionary<string, string> results = new Dictionary<string, string>();
            IEnumerable<dynamic> queryResults = await _repository
             .DynamicQuery(new CosmosDbQuery
             {
                 QueryText = @"
                                SELECT c.id as id, c.content.partitionKey as partitionKey FROM c
                                WHERE c.documentType = 'PublishedFunding'
                                AND c.content.current.fundingStreamId = @fundingStreamId
                                AND c.content.current.fundingPeriod.id = @fundingPeriodId",
                 Parameters = new[]
                 {
                                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                 }
             });

            foreach (dynamic item in queryResults)
            {
                results.Add((string)item.id, (string)item.partitionKey);
            }

            return await Task.FromResult(results);
        }

        public async Task<PublishedFunding> GetPublishedFundingById(string cosmosId, string partitionKey)
        {
            Guard.IsNullOrWhiteSpace(cosmosId, nameof(cosmosId));
            Guard.IsNullOrWhiteSpace(partitionKey, nameof(partitionKey));

            return (await _repository.ReadDocumentByIdPartitionedAsync<PublishedFunding>(cosmosId, partitionKey))?.Content;
        }
        
        public async Task DeleteAllPublishedProvidersByFundingStreamAndPeriod(string fundingStreamId, 
            string fundingPeriodId)
        {
            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"SELECT
                                        c.content.id,
                                        { 
                                           'id' : c.content.current.id,
                                           'providerId' : c.content.current.providerId,
                                           'fundingStreamId' : c.content.current.fundingStreamId,
                                           'fundingPeriodId' : c.content.current.fundingPeriodId
                                        } AS Current
                               FROM     publishedProvider c
                               WHERE    c.documentType = 'PublishedProvider'
                               AND      c.content.current.fundingStreamId = @fundingStreamId
                               AND      c.content.current.fundingPeriodId = @fundingPeriodId
                               AND      c.deleted = false",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                }
            };

            await _repository.DocumentsBatchProcessingAsync<PublishedProvider>(persistBatchToIndex: async matches =>
                {
                    await _repository.BulkDeleteAsync(matches.ToDictionary(_ => _.ParitionKey, _ => _));
                },
                cosmosDbQuery: query,
                itemsPerPage: 50);
        }

        public async Task DeleteAllPublishedProviderVersionsByFundingStreamAndPeriod(string fundingStreamId, 
            string fundingPeriodId)
        {
            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"SELECT
                                        c.content.id,
                                        { 
                                           'providerType' : c.content.provider.providerType,
                                           'localAuthorityName' : c.content.provider.localAuthorityName,
                                           'name' : c.content.provider.name
                                        } AS Provider,
                                        c.content.status,
                                        c.content.totalFunding,
                                        c.content.specificationId,
                                        c.content.fundingStreamId,
                                        c.content.providerId,
                                        c.content.fundingPeriodId,
                                        c.content.version,
                                        c.content.majorVersion,
                                        c.content.minorVersion
                               FROM     publishedProviderVersion c
                               WHERE    c.documentType = 'PublishedProviderVersion'
                               AND      c.content.fundingStreamId = @fundingStreamId
                               AND      c.content.fundingPeriodId = @fundingPeriodId
                               AND      c.deleted = false",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                }
            };

            await _repository.DocumentsBatchProcessingAsync<PublishedProviderVersion>(persistBatchToIndex: async matches =>
                {
                    await _repository.BulkDeleteAsync(matches.Select(_ => new KeyValuePair<string, PublishedProviderVersion>(_.PartitionKey, _)));
                },
                cosmosDbQuery: query,
                itemsPerPage: 50);
        }

        public async Task PublishedProviderBatchProcessing(string predicate,
            string specificationId,
            Func<List<PublishedProvider>, Task> persistIndexBatch,
            int batchSize)
        {
            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = $@"SELECT 
                                {{
                                    'id': c.content.current.id,
                                    'providerId' : c.content.current.providerId,
                                    'fundingStreamId' : c.content.current.fundingStreamId,
                                    'fundingPeriodId' : c.content.current.fundingPeriodId,
                                    'specificationId' : c.content.current.specificationId,
                                    'status'          : c.content.current.status,
                                    'totalFunding'    : c.content.current.totalFunding,
                                    'version'         : c.content.current.version,
                                    'majorVersion'    : c.content.current.majorVersion,
                                    'minorVersion'    : c.content.current.minorVersion,
                                    'date'            : c.content.current.date,
                                    'author'          : {{
                                        'name' : c.content.current.author.name
                                    }},
                                    'provider'        : {{ 
                                        'providerType' : c.content.current.provider.providerType,
                                        'localAuthorityName' : c.content.current.provider.localAuthorityName,
                                        'name' : c.content.current.provider.name,
                                        'ukprn' : c.content.current.provider.ukprn
                                    }},
                                    'fundingLines' : ARRAY(
                                        SELECT fundingLine.name,
                                               fundingLine['value']
                                        FROM fundingLine IN c.content.current.fundingLines
                                    )
                                }} AS Current
                               FROM     publishedProviders c
                               WHERE    c.documentType = 'PublishedProvider'
                               AND      c.content.current.specificationId = @specificationId
                               AND      {predicate} 
                               AND      c.deleted = false",
                Parameters = new []
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId), 
                }
            };

            await _repository.DocumentsBatchProcessingAsync(persistBatchToIndex: persistIndexBatch,
                cosmosDbQuery: query,
                itemsPerPage: batchSize);   
        }

        public async Task AllPublishedProviderBatchProcessing(Func<List<PublishedProvider>, Task> persistIndexBatch, int batchSize)
        {
            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"SELECT
                                        c.content.id,
                                        { 
                                           'id' : c.content.current.id,
                                           'providerId' : c.content.current.providerId,
                                           'fundingStreamId' : c.content.current.fundingStreamId,
                                           'fundingPeriodId' : c.content.current.fundingPeriodId,
                                           'specificationId' : c.content.current.specificationId,
                                           'status'          : c.content.current.status,
                                           'totalFunding'    : c.content.current.totalFunding,
                                           'version'         : c.content.current.version,
                                           'provider'        : { 
                                               'providerType' : c.content.current.provider.providerType,
                                               'localAuthorityName' : c.content.current.provider.localAuthorityName,
                                               'name' : c.content.current.provider.name,
                                               'ukprn' : c.content.current.provider.ukprn
                                            }
                                        } AS Current
                               FROM     publishedProviders c
                               WHERE    c.documentType = 'PublishedProvider' 
                               AND      c.deleted = false"
            };

            await _repository.DocumentsBatchProcessingAsync(persistBatchToIndex: persistIndexBatch,
                cosmosDbQuery: query,
                itemsPerPage: batchSize);
        }

        public async Task<IEnumerable<PublishedProviderFundingStreamStatus>> GetPublishedProviderStatusCounts(string specificationId, string providerType, string localAuthority, string status)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            List<PublishedProviderFundingStreamStatus> results = new List<PublishedProviderFundingStreamStatus>();

            StringBuilder additionalFilter = new StringBuilder();
            
            List<CosmosDbQueryParameter> cosmosDbQueryParameters = new List<CosmosDbQueryParameter>();
            cosmosDbQueryParameters.Add(new CosmosDbQueryParameter("@specificationId", specificationId));

            if (!string.IsNullOrWhiteSpace(providerType))
            {
                additionalFilter.Append($" and f.content.current.provider.providerType = @providerType ");
                cosmosDbQueryParameters.Add(new CosmosDbQueryParameter("@providerType", providerType));
            }

            if (!string.IsNullOrWhiteSpace(localAuthority))
            {
                additionalFilter.Append($" and f.content.current.provider.localAuthorityName = @localAuthorityName ");
                cosmosDbQueryParameters.Add(new CosmosDbQueryParameter("@localAuthorityName", localAuthority));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                additionalFilter.Append($" and f.content.current.status = @status ");
                cosmosDbQueryParameters.Add(new CosmosDbQueryParameter("@status", status));
            }

            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = $@"SELECT COUNT(1) AS count, f.content.current.fundingStreamId, f.content.current.status, SUM(f.content.current.totalFunding) AS totalFundingSum
                                FROM f
                                where f.documentType = 'PublishedProvider' and f.content.current.specificationId = @specificationId and f.deleted = false {additionalFilter.ToString()}
                                GROUP BY f.content.current.fundingStreamId, f.content.current.status",
                Parameters = cosmosDbQueryParameters
            };

            IEnumerable<dynamic> queryResults = await _repository
             .DynamicQuery(query);

            foreach (dynamic item in queryResults)
            {
                results.Add(new PublishedProviderFundingStreamStatus
                {
                    Count = (int)item.count,
                    FundingStreamId = (string)item.fundingStreamId,
                    Status = (string)item.status,
                    TotalFunding = (decimal)item.totalFundingSum
                });
            }

            return await Task.FromResult(results);
        }
    }
}