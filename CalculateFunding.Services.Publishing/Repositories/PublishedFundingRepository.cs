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
        private readonly IPublishedFundingQueryBuilder _publishedFundingQueryBuilder;

        public PublishedFundingRepository(ICosmosRepository cosmosRepository, 
            IPublishedFundingQueryBuilder publishedFundingQueryBuilder)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(publishedFundingQueryBuilder, nameof(publishedFundingQueryBuilder));

            _repository = cosmosRepository;
            _publishedFundingQueryBuilder = publishedFundingQueryBuilder;
        }

        public async Task<HttpStatusCode> UpsertPublishedProvider(PublishedProvider publishedProvider)
        {
            Guard.ArgumentNotNull(publishedProvider, nameof(publishedProvider));

            return await _repository.UpsertAsync(publishedProvider, publishedProvider.PartitionKey);
        }

        public async Task<IEnumerable<HttpStatusCode>> UpsertPublishedProviders(IEnumerable<PublishedProvider> publishedProviders)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));

            IEnumerable<Task<HttpStatusCode>> tasks = publishedProviders.Select(async (_) => await UpsertPublishedProvider(_));

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

        public async Task<PublishedProvider> GetPublishedProvider(string fundingStreamId,
            string fundingPeriodId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            return (await _repository
                .QuerySql<PublishedProvider>(new CosmosDbQuery
                {
                    QueryText = @"SELECT *
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
                .SingleOrDefault();
        }

        public async Task<PublishedProviderVersion> GetLatestPublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            string providerId)
        {
            return (await GetPublishedProvider(fundingStreamId, fundingPeriodId, providerId))?.Current;
        }

        public async Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersions(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            string status = null)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            StringBuilder publishedProviderVersionQry = new StringBuilder(@"SELECT *
                                 FROM c
                                 WHERE c.documentType = 'PublishedProviderVersion'
                                 AND c.deleted = false
                                 AND c.content.providerId = @providerId
                                 AND c.content.fundingStreamId = @fundingStreamId
                                 AND c.content.fundingPeriodId = @fundingPeriodId");

            IEnumerable<CosmosDbQueryParameter> parameters = new[]
                    {
                        new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                        new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId),
                        new CosmosDbQueryParameter("@providerId", providerId)
                    };

            if (status != null)
            {
                publishedProviderVersionQry.Append(" AND c.content.status = @status");
                parameters = parameters.Append(new CosmosDbQueryParameter("@status", status));
            }

            return (await _repository
                .QuerySql<PublishedProviderVersion>(new CosmosDbQuery
                {
                    QueryText = publishedProviderVersionQry.ToString(),
                    Parameters = parameters
                }));
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

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIdsForApproval(string fundingStreamId, string fundingPeriodId, string[] providerIds = null)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            StringBuilder queryTextBuilder = new StringBuilder(@"
                                SELECT c.id as id, c.content.partitionKey as partitionKey FROM c
                                WHERE c.documentType = 'PublishedProvider'
                                AND c.content.current.fundingStreamId = @fundingStreamId
                                AND c.content.current.fundingPeriodId = @fundingPeriodId
                                AND (c.content.current.status = 'Draft' OR c.content.current.status = 'Updated')");

            List<CosmosDbQueryParameter> cosmosDbQueryParameters = new List<CosmosDbQueryParameter>{
                new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
            };

            if (providerIds != null && providerIds.Any())
            {
                string providerIdQueryText = string.Join(',', providerIds.Select((_, index) => $"@providerId_{index}"));
                queryTextBuilder.Append($" AND c.content.current.providerId IN ({providerIdQueryText})");

                cosmosDbQueryParameters.AddRange(providerIds.Select((_, index) => new CosmosDbQueryParameter($"@providerId_{index}", _)));
            }

            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = queryTextBuilder.ToString(),
                Parameters = cosmosDbQueryParameters
            };

            Dictionary<string, string> results = new Dictionary<string, string>();
            IEnumerable<dynamic> queryResults = await _repository
             .DynamicQuery(cosmosDbQuery);

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

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIds(string fundingStreamId, string fundingPeriodId, string[] providerIds = null)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            StringBuilder queryTextBuilder = new StringBuilder(@"
                                SELECT c.id as id, c.content.partitionKey as partitionKey FROM c
                                WHERE c.documentType = 'PublishedProvider'
                                AND c.content.current.fundingStreamId = @fundingStreamId
                                AND c.content.current.fundingPeriodId = @fundingPeriodId");

            List<CosmosDbQueryParameter> cosmosDbQueryParameters = new List<CosmosDbQueryParameter>{
                new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
            };

            if (providerIds != null && providerIds.Any())
            {
                string providerIdQueryText = string.Join(',', providerIds.Select((_, index) => $"@providerId_{index}"));
                queryTextBuilder.Append($" AND c.content.current.providerId IN ({providerIdQueryText})");

                cosmosDbQueryParameters.AddRange(providerIds.Select((_, index) => new CosmosDbQueryParameter($"@providerId_{index}", _)));
            }

            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = queryTextBuilder.ToString(),
                Parameters = cosmosDbQueryParameters
            };

            Dictionary<string, string> results = new Dictionary<string, string>();
            IEnumerable<dynamic> queryResults = await _repository.DynamicQuery(cosmosDbQuery);

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

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingVersionIds(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            Dictionary<string, string> results = new Dictionary<string, string>();
            IEnumerable<dynamic> queryResults = await _repository
             .DynamicQuery(new CosmosDbQuery
             {
                 QueryText = @"
                                SELECT c.id as id, c.content.partitionKey as partitionKey FROM c
                                WHERE c.documentType = 'PublishedFundingVersion'
                                AND c.content.fundingStreamId = @fundingStreamId
                                AND c.content.fundingPeriod.id = @fundingPeriodId",
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

        public async Task<PublishedFundingVersion> GetPublishedFundingVersionById(string cosmosId, string partitionKey)
        {
            Guard.IsNullOrWhiteSpace(cosmosId, nameof(cosmosId));
            Guard.IsNullOrWhiteSpace(partitionKey, nameof(partitionKey));

            return (await _repository.ReadDocumentByIdPartitionedAsync<PublishedFundingVersion>(cosmosId, partitionKey))?.Content;
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
                               AND      c.content.current.fundingPeriodId = @fundingPeriodId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                }
            };

            await _repository.DocumentsBatchProcessingAsync<PublishedProvider>(persistBatchToIndex: async matches =>
                {
                    await _repository.BulkDeleteAsync(matches.ToDictionary(_ => _.PartitionKey, _ => _), hardDelete: true);
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
                               AND      c.content.fundingPeriodId = @fundingPeriodId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                }
            };

            await _repository.DocumentsBatchProcessingAsync<PublishedProviderVersion>(persistBatchToIndex: async matches =>
                {
                    await _repository.BulkDeleteAsync(matches.Select(_ => new KeyValuePair<string, PublishedProviderVersion>(_.PartitionKey, _)), hardDelete: true);
                },
                cosmosDbQuery: query,
                itemsPerPage: 50);
        }

        public async Task DeleteAllPublishedFundingsByFundingStreamAndPeriod(string fundingStreamId,
            string fundingPeriodId)
        {
            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"SELECT
                                    {
                                        'id':c.content.id,
                                        'current' : {
                                            'groupingReason':c.content.current.groupingReason,
                                            'organisationGroupTypeCode':c.content.current.organisationGroupTypeCode,
                                            'organisationGroupIdentifierValue':c.content.current.organisationGroupIdentifierValue,
                                            'fundingStreamId':c.content.current.fundingStreamId,
                                            'fundingPeriod':{
                                                        'id':c.content.current.fundingPeriod.id,
                                                        'type':c.content.current.fundingPeriod.type,
                                                        'period':c.content.current.fundingPeriod.period
                                            }
                                        }
                                    } AS content
                               FROM     publishedFunding c
                               WHERE    c.documentType = 'PublishedFunding'
                               AND      c.content.current.fundingStreamId = @fundingStreamId
                               AND      c.content.current.fundingPeriod.id = @fundingPeriodId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                }
            };

            await _repository.DocumentsBatchProcessingAsync<DocumentEntity<PublishedFunding>>(persistBatchToIndex: async matches =>
            {
                await _repository.BulkDeleteAsync(matches.Select(_ => new KeyValuePair<string, PublishedFunding>(_.Content.ParitionKey, _.Content)), hardDelete: true);
            },
            cosmosDbQuery: query,
            itemsPerPage: 50);
        }

        public async Task DeleteAllPublishedFundingVersionsByFundingStreamAndPeriod(string fundingStreamId,
            string fundingPeriodId)
        {
            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"SELECT
                                    { 
                                        'id':c.content.id,
                                        'groupingReason':c.content.groupingReason,
                                        'organisationGroupTypeCode':c.content.organisationGroupTypeCode,
                                        'organisationGroupIdentifierValue':c.content.organisationGroupIdentifierValue,
                                        'version':c.content.version,
                                        'fundingStreamId':c.content.fundingStreamId,
                                        'fundingPeriod':{
                                            'id':c.content.fundingPeriod.id,
                                            'type':c.content.fundingPeriod.type,
                                            'period':c.content.fundingPeriod.period
                                        }
                                    } as content
                               FROM     publishedFundingVersion c
                               WHERE    c.documentType = 'PublishedFundingVersion'
                               AND      c.content.fundingStreamId = @fundingStreamId
                               AND      c.content.fundingPeriod.id = @fundingPeriodId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                }
            };

            await _repository.DocumentsBatchProcessingAsync<DocumentEntity<PublishedFundingVersion>>(persistBatchToIndex: async matches =>
            {
                await _repository.BulkDeleteAsync(matches.Select(_ => new KeyValuePair<string, PublishedFundingVersion>(_.Content.PartitionKey, _.Content)), hardDelete: true);
            },
            cosmosDbQuery: query,
            itemsPerPage: 50);
        }


        public async Task PublishedProviderVersionBatchProcessing(string predicate,
            string specificationId,
            Func<List<PublishedProviderVersion>, Task> batchProcessor,
            int batchSize,
            string joinPredicate = null,
            string fundingLineCode = null)
        {
                CosmosDbQuery query = new CosmosDbQuery
                {
                    QueryText = $@"SELECT 
                                c.content.id,
                                c.content.providerId,
                                c.content.fundingStreamId,
                                c.content.fundingPeriodId,
                                c.content.specificationId,
                                c.content.status,
                                c.content.totalFunding,
                                c.content.version,
                                c.content.majorVersion,
                                c.content.minorVersion,
                                c.content.date,
                                {{
                                    'name' : c.content.author.name
                                }} AS Author,
                                {{ 
                                    'providerType' : c.content.provider.providerType,
                                    'providerSubType' : c.content.provider.providerSubType,
                                    'localAuthorityName' : c.content.provider.localAuthorityName,
                                    'laCode' : c.content.provider.laCode,
                                    'name' : c.content.provider.name,
                                    'ukprn' : c.content.provider.ukprn,
                                    'urn' : c.content.provider.urn,
                                    'establishmentNumber' : c.content.provider.establishmentNumber
                                }} AS Provider,
                               ARRAY(
                                    SELECT fundingLine.name,
                                    fundingLine['value'],
                                    ARRAY(
                                        SELECT distributionPeriod['value'],
                                        ARRAY(
                                            SELECT profilePeriod.year,
                                            profilePeriod.typeValue,
                                            profilePeriod.occurrence,
                                            profilePeriod.profiledValue
                                            FROM profilePeriod IN distributionPeriod.profilePeriods
                                        ) AS profilePeriods
                                        FROM distributionPeriod IN fundingLine.distributionPeriods
                                    ) AS distributionPeriods
                                    FROM fundingLine IN c.content.fundingLines {joinPredicate}
                                ) AS FundingLines
                                FROM     publishedProviderVersions c
                                WHERE    c.documentType = 'PublishedProviderVersion'
                                AND      c.content.specificationId = @specificationId
                                AND      {predicate} 
                                AND      c.deleted = false
                                ORDER BY c.content.provider.ukprn",
                    Parameters = new []
                    {
                        new CosmosDbQueryParameter("@specificationId", specificationId),
                        new CosmosDbQueryParameter("@fundingLineCode", fundingLineCode),
                    }
                };

            await _repository.DocumentsBatchProcessingAsync(persistBatchToIndex: batchProcessor,
                cosmosDbQuery: query,
                itemsPerPage: batchSize);      
        }
        
        public async Task PublishedProviderBatchProcessing(string predicate,
            string specificationId,
            Func<List<PublishedProvider>, Task> batchProcessor,
            int batchSize,
            string joinPredicate = null,
            string fundingLineCode = null)
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
                                        'providerSubType' : c.content.current.provider.providerSubType,
                                        'localAuthorityName' : c.content.current.provider.localAuthorityName,
                                        'laCode' : c.content.current.provider.laCode,
                                        'name' : c.content.current.provider.name,
                                        'ukprn' : c.content.current.provider.ukprn,
                                        'urn' : c.content.current.provider.urn,
                                        'establishmentNumber' : c.content.current.provider.establishmentNumber
                                    }},
                                    'fundingLines' : ARRAY(
                                        SELECT fundingLine.name,
                                        fundingLine['value'],
                                        ARRAY(
                                            SELECT distributionPeriod['value'],
                                            ARRAY(
                                                SELECT profilePeriod.year,
                                                profilePeriod.typeValue,
                                                profilePeriod.occurrence,
                                                profilePeriod.profiledValue
                                                FROM profilePeriod IN distributionPeriod.profilePeriods
                                            ) AS profilePeriods
                                            FROM distributionPeriod IN fundingLine.distributionPeriods
                                        ) AS distributionPeriods
                                        FROM fundingLine IN c.content.current.fundingLines  {joinPredicate}
                                    )
                                }} AS Current
                               FROM     publishedProviders c
                               WHERE    c.documentType = 'PublishedProvider'
                               AND      c.content.current.specificationId = @specificationId
                               AND      {predicate} 
                               AND      c.deleted = false
                               ORDER BY c.content.current.provider.ukprn",
                Parameters = new []
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                    new CosmosDbQueryParameter("@fundingLineCode", fundingLineCode),
                }
            };

            await _repository.DocumentsBatchProcessingAsync(persistBatchToIndex: batchProcessor,
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
                                           'majorVersion' : c.content.current.majorVersion,
                                           'minorVersion' : c.content.current.minorVersion,
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

        public async Task RefreshedProviderVersionBatchProcessing(
            string specificationId, Func<List<PublishedProviderVersion>, Task> persistIndexBatch, int batchSize)
        {
            List<CosmosDbQueryParameter> cosmosDbQueryParameters = new List<CosmosDbQueryParameter>
            {
                new CosmosDbQueryParameter("@specificationId", specificationId)
            };

            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"SELECT 
                                        c.content.id,
                                        c.content.variationReasons,
                                        c.content.providerId,
                                        c.content.status,
                                        c.content.version,
                                        {
                                            'ukprn' : c.content.provider.ukprn,
                                            'successor' : c.content.provider.successor,
                                            'dateClosed' : c.content.provider.dateClosed,
                                            'dateOpened' : c.content.provider.dateOpened,
                                            'urn' : c.content.provider.urn,
                                            'name' : c.content.provider.name,
                                            'providerType': c.content.provider.providerType,
                                            'providerSubType': c.content.provider.providerSubType,
                                            'laCode' : c.content.provider.laCode,
                                            'localAuthorityName' : c.content.provider.localAuthorityName,
                                            'reasonEstablishmentOpened' : c.content.provider.reasonEstablishmentOpened,
                                            'reasonEstablishmentClosed' : c.content.provider.reasonEstablishmentClosed,
                                            'trustCode' : c.content.provider.trustCode,
                                            'trustName' : c.content.provider.trustName
                                        } AS Provider
                                FROM publishedProviders c
                                WHERE c.documentType = 'PublishedProviderVersion' 
                                AND c.content.specificationId = @specificationId 
                                AND (c.content.status = 'Updated' OR c.content.status = 'Released')
                                AND c.deleted = false
                                ORDER BY c.content.providerId",
                Parameters = cosmosDbQueryParameters
            };

            await _repository.DocumentsBatchProcessingAsync(persistBatchToIndex: persistIndexBatch,
                cosmosDbQuery: query,
                itemsPerPage: batchSize);
        }

        public async Task<IEnumerable<string>> GetPublishedProviderFundingLines(string specificationId, GroupingReason fundingLineType)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            List<string> fundingLines = new List<string>();
            IEnumerable<dynamic> queryResults = await _repository
             .DynamicQuery(new CosmosDbQuery
             {
                 QueryText = @"
                                SELECT DISTINCT VALUE  f.name
                                FROM publishedProviders p
                                JOIN f IN p.content.current.fundingLines
                                WHERE    p.documentType = 'PublishedProvider'
                                AND      f.type = @fundingLineType
                                AND      p.content.current.specificationId = @specificationId
                                AND      p.deleted = false",
                 Parameters = new[]
                 {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                    new CosmosDbQueryParameter("@fundingLineType", fundingLineType.ToString()),
                 }
             });

            foreach (dynamic item in queryResults)
            {
                fundingLines.Add((string)item);
            }

            return await Task.FromResult(fundingLines.Distinct()); ;
        }

        public async Task<int> QueryPublishedFundingCount(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons)
        {
            CosmosDbQuery query = _publishedFundingQueryBuilder.BuildCountQuery(fundingStreamIds,
                fundingPeriodIds,
                groupingReasons);

            IEnumerable<dynamic> dynamicQuery = await _repository.DynamicQuery(query);

            return Convert.ToInt32(dynamicQuery
                .Single());
        }

        public async Task<IEnumerable<PublishedFundingIndex>> QueryPublishedFunding(IEnumerable<string> fundingStreamIds, 
            IEnumerable<string> fundingPeriodIds, 
            IEnumerable<string> groupingReasons, 
            int top, 
            int? pageRef)
        {
            CosmosDbQuery query = _publishedFundingQueryBuilder.BuildQuery(fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                top,
                pageRef);

            return (await _repository.DynamicQuery(query))
                .Select(_ => new PublishedFundingIndex
                {
                    Id = _.id,
                    Deleted = _.deleted,
                    DocumentPath = _.DocumentPath,
                    FundingPeriodId = _.FundingPeriodId,
                    FundingStreamId = _.fundingStreamId,
                    GroupTypeIdentifier = _.GroupTypeIdentifier,
                    GroupingType = _.GroupingType,
                    IdentifierValue = _.IdentifierValue,
                    Version = _.version,
                    StatusChangedDate = _.statusChangedDate
                });
        }
    }
}