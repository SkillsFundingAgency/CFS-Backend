using System;
using System.Collections.Generic;
using System.Globalization;
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
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

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

            return await _repository.UpsertAsync(publishedProvider, publishedProvider.PartitionKey, undelete: true);
        }

        public async Task<IEnumerable<HttpStatusCode>> UpsertPublishedProviders(IEnumerable<PublishedProvider> publishedProviders)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));

            IEnumerable<Task<HttpStatusCode>> tasks = publishedProviders.Select(async (_) => await UpsertPublishedProvider(_));

            await TaskHelper.WhenAllAndThrow(tasks.ToArray());

            return tasks.Select(_ => _.Result);
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = _repository.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(PublishedFundingRepository)
            };

            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = Ok,
                DependencyName = _repository.GetType().GetFriendlyName(),
                Message = Message
            });

            return Task.FromResult(health);
        }

        public async Task<DateTime?> GetLatestPublishedDate(string fundingStreamId,
            string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            IEnumerable<dynamic> dynamicQuery = (await _repository.DynamicQuery(new CosmosDbQuery
            {
                QueryText = @"SELECT MAX(c.updatedAt)
                                  FROM c
                                  WHERE c.documentType = 'PublishedProvider'
                                  AND c.deleted = false
                                  AND c.content.current.fundingStreamId = @fundingStreamId
                                  AND c.content.current.fundingPeriodId = @fundingPeriodId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId),
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId)
                }
            }));

            JToken dynamic = dynamicQuery
                .SingleOrDefault();

            return dynamic?.HasValues == true ? dynamic.First.ToObject<DateTime>() : (DateTime?)null;
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

        public async Task<PublishedProvider> GetPublishedProviderBySpecificationId(
            string specificationId,
            string fundingStreamId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
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
                                 AND c.content.current.specificationId = @specificationId",
                    Parameters = new[]
                    {
                        new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                        new CosmosDbQueryParameter("@specificationId", specificationId),
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

        public async Task<PublishedProviderVersion> GetLatestPublishedProviderVersionBySpecificationId(
            string specificationId,
            string fundingStreamId,
            string providerId)
        {
            return (await GetPublishedProviderBySpecificationId(specificationId, fundingStreamId, providerId))?.Current;
        }

        public async Task<PublishedProviderVersion> GetPublishedProviderVersionById(string publishedProviderVersionId)
        {
            return (await _repository.ReadDocumentByIdAsync<PublishedProviderVersion>(publishedProviderVersionId))?.Content;
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

        public async Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersionsForApproval(
            string specificationId,
            string fundingStreamId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            return await _repository
             .QuerySql<PublishedProviderVersion>(new CosmosDbQuery
             {
                 QueryText = @"SELECT * FROM c
                                WHERE c.content.specificationId = @specificationId
                                AND c.content.providerId = @providerId
                                AND c.content.fundingStreamId = @fundingStreamId
                                AND c.deleted = false
                                AND c.documentType = 'PublishedProviderVersion'
                                AND (c.content.status = 'Draft' OR c.content.status = 'Updated')
                                ORDER BY c.content.date desc",
                 Parameters = new[]
                 {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
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

            string id = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}-{version}";
            string partitionKey = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}";

            return (await _repository.TryReadDocumentByIdPartitionedAsync<PublishedProviderVersion>(id, partitionKey))?.Content;
        }

        public async Task<HttpStatusCode> UpsertPublishedFunding(PublishedFunding publishedFunding)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));

            return await _repository.UpsertAsync(publishedFunding, publishedFunding.ParitionKey, undelete: true);
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIdsForApproval(string fundingStreamId, string fundingPeriodId, string[] publishedProviderIds = null)
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

            if (publishedProviderIds != null && publishedProviderIds.Any())
            {
                string publishedProviderIdQueryText = string.Join(',', publishedProviderIds.Select((_, index) => $"@publishedProviderId_{index}"));
                queryTextBuilder.Append($" AND c.content.publishedProviderId IN ({publishedProviderIdQueryText})");

                cosmosDbQueryParameters.AddRange(publishedProviderIds.Select((_, index) => new CosmosDbQueryParameter($"@publishedProviderId_{index}", _)));
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

            return results;
        }

        public async Task<PublishedProvider> GetPublishedProviderById(string cosmosId, string partitionKey)
        {
            Guard.IsNullOrWhiteSpace(cosmosId, nameof(cosmosId));
            Guard.IsNullOrWhiteSpace(partitionKey, nameof(partitionKey));

            return (await _repository.ReadDocumentByIdPartitionedAsync<PublishedProvider>(cosmosId, partitionKey))?.Content;
        }

        public async Task<IDictionary<string, string>> GetPublishedProviderIdsForUkprns(string fundingStreamId,
            string fundingPeriodId,
            string[] ukprns)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNotEmpty(ukprns, nameof(ukprns));

            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"
                              SELECT 
                                  c.content.current.publishedProviderId as publishedProviderId,
                                  c.content.current.provider.ukprn as ukprn
                              FROM publishedProvider c
                              WHERE c.documentType = 'PublishedProvider'
                              AND c.deleted = false 
                              AND c.content.current.fundingStreamId = @fundingStreamId
                              AND c.content.current.fundingPeriodId = @fundingPeriodId
                              AND ARRAY_CONTAINS(@ukprns, c.content.current.provider.ukprn) 
                              AND c.deleted = false",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId),
                    new CosmosDbQueryParameter("@ukprns", ukprns),
                }
            };

            IEnumerable<dynamic> results = await _repository.DynamicQuery(query);

            return results
                .Select(_ => ((string)_.publishedProviderId, (string)_.ukprn))
                .ToDictionary(_ => _.Item2, _ => _.Item1);
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIds(string fundingStreamId, string fundingPeriodId, string[] providerIds = null)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            StringBuilder queryTextBuilder = new StringBuilder(@"
                                SELECT c.id as id, c.content.partitionKey as partitionKey FROM c
                                WHERE c.documentType = 'PublishedProvider'
                                AND c.content.current.fundingStreamId = @fundingStreamId
                                AND c.content.current.fundingPeriodId = @fundingPeriodId
                                AND c.deleted = false");

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

            return results;
        }

        public async Task<IEnumerable<string>> GetPublishedProviderIds(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            StringBuilder queryTextBuilder = new StringBuilder(@"
                                SELECT c.id as id FROM c
                                WHERE c.documentType = 'PublishedProvider'
                                AND c.content.current.specificationId = @specificationId");

            List<CosmosDbQueryParameter> cosmosDbQueryParameters = new List<CosmosDbQueryParameter>{
                new CosmosDbQueryParameter("@specificationId", specificationId),
            };

            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = queryTextBuilder.ToString(),
                Parameters = cosmosDbQueryParameters
            };

            List<string> results = new List<string>();
            IEnumerable<dynamic> queryResults = await _repository.DynamicQuery(cosmosDbQuery);

            foreach (dynamic item in queryResults)
            {
                results.Add((string)item.id);
            }

            return results;
        }

        public async Task<IEnumerable<string>> GetPublishedProviderPublishedProviderIds(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT c.content.current.publishedProviderId as id FROM c
                                WHERE c.documentType = 'PublishedProvider'
                                AND c.content.current.specificationId = @specificationId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                }
            };

            List<string> results = new List<string>();
            IEnumerable<dynamic> queryResults = await _repository.DynamicQuery(cosmosDbQuery);

            foreach (dynamic item in queryResults)
            {
                results.Add((string)item.id);
            }

            return results;
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

            return results;
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingIds(string specificationId, GroupingReason? groupReason = null)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            Dictionary<string, string> results = new Dictionary<string, string>();

            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"SELECT c.id as id, c.content.partitionKey as partitionKey FROM c
                              WHERE c.documentType = 'PublishedFunding'
                              AND c.content.current.specificationId = @specificationId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId)
                }
            };

            if (groupReason.HasValue)
            {
                query.QueryText += " AND c.content.current.groupingReason = @groupReason";
                query.Parameters = query.Parameters.Concat(new[]
                {
                    new CosmosDbQueryParameter("@groupReason", groupReason.ToString())
                });
            }

            IEnumerable<dynamic> queryResults = await _repository
             .DynamicQuery(query);

            foreach (dynamic item in queryResults)
            {
                results.Add((string)item.id, (string)item.partitionKey);
            }

            return results;
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

            return results;
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

        public ICosmosDbFeedIterator<PublishedFunding> GetPublishedFundingForBatchProcessing(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            int batchSize)
        {
            CosmosDbQuery query = CreateQueryForPublishedFundingBatchProcessing(specificationId, fundingStreamId, fundingPeriodId);

            return _repository.GetFeedIterator<PublishedFunding>(query, batchSize);
        }

        private static CosmosDbQuery CreateQueryForPublishedFundingBatchProcessing(string specificationId,
            string fundingStreamId,
            string fundingPeriodId)
        {
            return new CosmosDbQuery
            {
                QueryText = @"SELECT 
                                {
                                    'id':c.content.id,
                                    'current':{
                                        'organisationGroupTypeCode' : c.content.current.organisationGroupTypeCode,
                                        'groupingReason' : c.content.current.groupingReason,
                                        'organisationGroupName' : c.content.current.organisationGroupName,
                                        'organisationGroupIdentifierValue' : c.content.current.organisationGroupIdentifierValue,
                                        'fundingStreamId' : c.content.current.fundingStreamId,
                                        'fundingPeriod' : {
                                          'id' : c.content.current.fundingPeriod.id
                                        },
                                        'specificationId' : c.content.current.specificationId,
                                        'status' : c.content.current.status,
                                        'version' : c.content.current.version,
                                        'majorVersion' : c.content.current.majorVersion,
                                        'minorVersion' : c.content.current.minorVersion,
                                        'date' : c.content.current.date,
                                        'providerFundings' : c.content.current.providerFundings,
                                        'author' : {
                                            'name' : c.content.current.author.name
                                        },
                                        'fundingLines' : ARRAY(
                                            SELECT fundingLine.name,
                                            fundingLine['value']
                                            FROM fundingLine IN c.content.current.fundingLines
                                        )
                                    }
                                } as content
                                FROM     publishedFunding c
                                WHERE    c.documentType = 'PublishedFunding'
                                AND      c.content.current.specificationId = @specificationId
                                AND      c.content.current.fundingPeriod.id = @fundingPeriodId
                                AND      c.content.current.fundingStreamId = @fundingStreamId
                                AND      c.deleted = false
                                ORDER BY c.content.current.organisationGroupTypeCode ASC,
                                c.content.current.date DESC",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                }
            };
        }

        public ICosmosDbFeedIterator<PublishedFundingVersion> GetPublishedFundingVersionsForBatchProcessing(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            int batchSize)
        {
            CosmosDbQuery query = CreateQueryForPublishedFundingVersionBatchProcessing(specificationId, fundingStreamId, fundingPeriodId);

            return _repository.GetFeedIterator<PublishedFundingVersion>(query, batchSize);
        }

        private static CosmosDbQuery CreateQueryForPublishedFundingVersionBatchProcessing(string specificationId,
            string fundingStreamId,
            string fundingPeriodId)
        {
            return new CosmosDbQuery
            {
                QueryText = @"SELECT 
                                {
                                    'id':c.content.id,
                                    'organisationGroupTypeCode':c.content.organisationGroupTypeCode,
                                    'organisationGroupName':c.content.organisationGroupName,
                                    'fundingStreamId':c.content.fundingStreamId,
                                    'fundingPeriod':{
                                          'id' : c.content.fundingPeriod.id
                                    },
                                    'specificationId':c.content.specificationId,
                                    'status':c.content.status,
                                    'version':c.content.version,
                                    'majorVersion':c.content.majorVersion,
                                    'minorVersion':c.content.minorVersion,
                                    'date':c.content.date,
                                    'author':{
                                        'name' : c.content.author.name
                                    },
                                    'fundingLines':ARRAY(
                                        SELECT fundingLine.name,
                                        fundingLine['value']
                                        FROM fundingLine IN c.content.fundingLines
                                    ),
                                    'providerFundings':c.content.providerFundings
                                } as content
                                FROM     publishedFundingVersions c
                                WHERE    c.documentType = 'PublishedFundingVersion'
                                AND      c.content.specificationId = @specificationId
                                AND      c.content.fundingPeriod.id = @fundingPeriodId
                                AND      c.content.fundingStreamId = @fundingStreamId
                                AND      c.deleted = false
                                ORDER BY c.content.organisationGroupTypeCode ASC,
                                c.content.date DESC",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId)
                }
            };
        }

        public ICosmosDbFeedIterator<PublishedProviderVersion> GetPublishedProviderVersionsForBatchProcessing(string predicate,
            string specificationId,
            int batchSize,
            string joinPredicate = null,
            string fundingLineName = null)
        {
            CosmosDbQuery query = CreateQueryForPublishedProviderVersionBatchProcessing(predicate, specificationId, joinPredicate, fundingLineName);

            return _repository.GetFeedIterator<PublishedProviderVersion>(query, batchSize);
        }

        private static CosmosDbQuery CreateQueryForPublishedProviderVersionBatchProcessing(string predicate,
            string specificationId,
            string joinPredicate,
            string fundingLineName)
        {
            return new CosmosDbQuery
            {
                QueryText = $@"SELECT 
                                    {{
                                        'id':c.content.id,
                                        'providerId':c.content.providerId,
                                        'fundingStreamId':c.content.fundingStreamId,
                                        'fundingPeriodId':c.content.fundingPeriodId,
                                        'specificationId':c.content.specificationId,
                                        'status':c.content.status,
                                        'totalFunding':c.content.totalFunding,
                                        'version':c.content.version,
                                        'majorVersion':c.content.majorVersion,
                                        'minorVersion':c.content.minorVersion,
                                        'date':c.content.date,
                                        'author':{{
                                            'name' : c.content.author.name
                                        }},
                                        'provider':{{ 
                                            'providerType' : c.content.provider.providerType,
                                            'providerSubType' : c.content.provider.providerSubType,
                                            'localAuthorityName' : c.content.provider.localAuthorityName,
                                            'laCode' : c.content.provider.laCode,
                                            'name' : c.content.provider.name,
                                            'ukprn' : c.content.provider.ukprn,
                                            'urn' : c.content.provider.urn,
                                            'establishmentNumber' : c.content.provider.establishmentNumber
                                        }},
                                        'fundingLines':ARRAY(
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
                                        )
                                    }} as content
                                FROM     publishedProviderVersions c
                                WHERE    c.documentType = 'PublishedProviderVersion'
                                AND      c.content.specificationId = @specificationId
                                AND      {predicate} 
                                AND      c.deleted = false
                                ORDER BY c.content.provider.ukprn",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                    new CosmosDbQueryParameter("@fundingLineName", fundingLineName)
                }
            };
        }

        public async Task PublishedProviderBatchProcessing(string predicate,
            string specificationId,
            Func<List<PublishedProvider>, Task> batchProcessor,
            int batchSize,
            string joinPredicate = null,
            string fundingLineName = null)
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
                                        'establishmentNumber' : c.content.current.provider.establishmentNumber,
                                        'status' : c.content.current.provider.status,
										'successor' : c.content.current.provider.successor
                                    }},
                                    'predecessors' : c.content.current.predecessors,
                                    'variationReasons' : c.content.current.variationReasons,
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
                                }} AS current,
                                {{
                                    'id': c.content.released.id,
                                    'providerId' : c.content.released.providerId,
                                    'fundingStreamId' : c.content.released.fundingStreamId,
                                    'fundingPeriodId' : c.content.released.fundingPeriodId,
                                    'specificationId' : c.content.released.specificationId,
                                    'status'          : c.content.released.status,
                                    'totalFunding'    : c.content.released.totalFunding,
                                    'version'         : c.content.released.version,
                                    'majorVersion'    : c.content.released.majorVersion,
                                    'minorVersion'    : c.content.released.minorVersion,
                                    'date'            : c.content.released.date,
                                    'author'          : {{
                                        'name' : c.content.released.author.name
                                    }},
                                    'provider'        : {{ 
                                        'providerType' : c.content.released.provider.providerType,
                                        'providerSubType' : c.content.released.provider.providerSubType,
                                        'localAuthorityName' : c.content.released.provider.localAuthorityName,
                                        'laCode' : c.content.released.provider.laCode,
                                        'name' : c.content.released.provider.name,
                                        'ukprn' : c.content.released.provider.ukprn,
                                        'urn' : c.content.released.provider.urn,
                                        'establishmentNumber' : c.content.released.provider.establishmentNumber,
                                        'status' : c.content.released.provider.status,
										'successor' : c.content.released.provider.successor
                                    }},
                                    'predecessors' : c.content.released.predecessors,
                                    'variationReasons' : c.content.released.variationReasons,
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
                                        FROM fundingLine IN c.content.released.fundingLines  {joinPredicate}
                                    )
                                }} AS released
                               FROM     publishedProviders c
                               WHERE    c.documentType = 'PublishedProvider'
                               AND      c.content.current.specificationId = @specificationId
                               AND      {predicate} 
                               AND      c.deleted = false
                               ORDER BY c.content.current.provider.ukprn",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                    new CosmosDbQueryParameter("@fundingLineName", fundingLineName)
                }
            };

            await _repository.DocumentsBatchProcessingAsync(persistBatchToIndex: batchProcessor,
                cosmosDbQuery: query,
                itemsPerPage: batchSize);
        }

        public async Task PublishedGroupBatchProcessing(string specificationId,
            Func<List<PublishedFunding>, Task> batchProcessor,
            int batchSize)
        {
            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = $@"SELECT {{
                                'fundingId': c.content.current.fundingId,
                                'majorVersion': c.content.current.majorVersion,
                                'groupingReason': c.content.current.groupingReason,
                                'organisationGroupTypeCode' : c.content.current.organisationGroupTypeCode,
                                'organisationGroupName' : c.content.current.organisationGroupName,
                                'organisationGroupTypeIdentifier' : c.content.current.organisationGroupTypeIdentifier,
                                'organisationGroupIdentifierValue' : c.content.current.organisationGroupIdentifierValue,
                                'organisationGroupTypeClassification' : c.content.current.organisationGroupTypeClassification,
                                'totalFunding' : c.content.current.totalFunding,
                                'author' : {{
                                                'name' : c.content.current.author.name
                                            }},
                                'statusChangedDate' : c.content.current.statusChangedDate,
                                'providerFundings' : c.content.current.providerFundings
                                }} AS current                                
                                FROM c 
                                where c.documentType='PublishedFunding'
                                and c.content.current.status = 'Released'
                                and c.content.current.specificationId = @specificationId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId)
                }
            };

            await _repository.DocumentsBatchProcessingAsync(persistBatchToIndex: batchProcessor,
               cosmosDbQuery: query,
               itemsPerPage: batchSize);
        }

        public async Task<IEnumerable<PublishedProvider>> QueryPublishedProvider(string specificationId, IEnumerable<string> fundingIds)
        {
            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = $@"SELECT 
                            c.content.released.fundingId,
                            c.content.released.fundingStreamId,
                            c.content.released.fundingPeriodId,
                            c.content.released.provider.providerId,
                            c.content.released.provider.name AS providerName,
                            c.content.released.majorVersion,
                            c.content.released.minorVersion,
                            c.content.released.totalFunding,
                            c.content.released.provider.ukprn,
                            c.content.released.provider.urn,
                            c.content.released.provider.upin,
                            c.content.released.provider.laCode,
                            c.content.released.provider.status,
                            c.content.released.provider.successor,
                            c.content.released.predecessors,
                            c.content.released.variationReasons
                        FROM c where c.documentType='PublishedProvider' 
                        and c.content.released.specificationId = @specificationId
                        and ARRAY_CONTAINS (@fundingIds, c.content.released.fundingId)",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                     new CosmosDbQueryParameter("@fundingIds", fundingIds)
                }
            };

            return (await _repository.DynamicQuery(query))
                .Select(_ => new PublishedProvider()
                {
                    Released = new PublishedProviderVersion
                    {
                        FundingStreamId = _.fundingStreamId,
                        FundingPeriodId = _.fundingPeriodId,
                        ProviderId = _.providerId,
                        Provider = new Provider { ProviderId = _.providerId, Name = _.providerName },
                        MajorVersion = _.majorVersion,
                        MinorVersion = _.minorVersion,
                        TotalFunding = _.totalFunding,
                    }
                });
        }

        public async Task AllPublishedProviderBatchProcessing(Func<List<PublishedProvider>, Task> persistIndexBatch, int batchSize, string specificationId)
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
                                               'providerSubType' : c.content.current.provider.providerSubType,
                                               'localAuthorityName' : c.content.current.provider.localAuthorityName,
                                               'authority' : c.content.current.provider.authority,
                                               'name' : c.content.current.provider.name,
                                               'ukprn' : c.content.current.provider.ukprn,
                                               'urn' : c.content.current.provider.urn,
                                               'upin' : c.content.current.provider.upin
                                            }
                                        } AS Current
                               FROM     publishedProviders c
                               WHERE    c.documentType = 'PublishedProvider' 
                               AND      c.deleted = false"
            };

            if (!string.IsNullOrWhiteSpace(specificationId))
            {
                query.QueryText += " AND c.content.current.specificationId = @specificationId";
                query.Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId)
                };
            }

            await _repository.DocumentsBatchProcessingAsync(persistBatchToIndex: persistIndexBatch,
                cosmosDbQuery: query,
                itemsPerPage: batchSize);
        }

        /// <summary>
        ///     Get count and sum of total funding for all published provider ids with the supplied status
        ///     NB ensure that the ids count is not greater than 100 as this is max permitted per query for an IN
        ///     clause in cosmos sql
        /// </summary>
        /// <param name="publishedProviderIds">the ids from the batch of published providers</param>
        /// <param name="specificationId"></param>
        /// <param name="statuses">the statuses to restrict to</param>
        /// <returns></returns>
        public async Task<IEnumerable<PublishedProviderFunding>> GetPublishedProvidersFunding(IEnumerable<string> publishedProviderIds,
            string specificationId,
            params PublishedProviderStatus[] statuses)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNotEmpty(publishedProviderIds, nameof(publishedProviderIds));
            Guard.Ensure(publishedProviderIds.Count() <= 100, "You can only filter against 100 published provider ids at a time");
            Guard.IsNotEmpty(statuses, nameof(statuses));

            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"
                              SELECT 
                                  c.content.current.specificationId,
                                  c.content.current.publishedProviderId,
                                  c.content.current.fundingStreamId,
                                  c.content.current.totalFunding,
                                  c.content.current.provider.providerType,
                                  c.content.current.provider.providerSubType, 
                                  c.content.current.provider.laCode
                              FROM publishedProvider c
                              WHERE c.documentType = 'PublishedProvider'
                              AND c.content.current.specificationId = @specificationId
                              AND ARRAY_CONTAINS(@publishedProviderIds, c.content.current.publishedProviderId) 
                              AND ARRAY_CONTAINS(@statuses, c.content.current.status)
                              AND (IS_NULL(c.content.current.errors) OR ARRAY_LENGTH(c.content.current.errors) = 0)
                              AND c.deleted = false",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                    new CosmosDbQueryParameter("@publishedProviderIds", publishedProviderIds.ToArray()),
                    new CosmosDbQueryParameter("@statuses", statuses?.Select(_ => _.ToString()).ToArray())
                }
            };

            IEnumerable<dynamic> results = await _repository.DynamicQuery(query);

            return results.Select(_ => new PublishedProviderFunding
            {
                SpecificationId = (string)_.specificationId,
                PublishedProviderId = (string)_.publishedProviderId,
                FundingStreamId = (string)_.fundingStreamId,
                TotalFunding = (decimal?)_.totalFunding,
                ProviderTypeSubType = new ProviderTypeSubType()
                {
                    ProviderType = (string)_.providerType,
                    ProviderSubType = (string)_.providerSubType
                },
                LaCode = (string)_.laCode
            });
        }

        public async Task<IEnumerable<string>> RemoveIdsInError(IEnumerable<string> publishedProviderIds)
        {
            CosmosDbQueryParameter providerIdsParameter = new CosmosDbQueryParameter("@publishedProviderIds", null);

            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"
                              SELECT 
                                  c.content.current.publishedProviderId
                              FROM publishedProvider c
                              WHERE c.documentType = 'PublishedProvider'
                              AND ARRAY_CONTAINS(@publishedProviderIds, c.content.current.publishedProviderId)
                              AND c.deleted = false
                              AND (IS_NULL(c.content.current.errors) OR ARRAY_LENGTH(c.content.current.errors) = 0)",
                Parameters = new[]
                {
                    providerIdsParameter
                }
            };

            PagedContext<string> publishedProviderIdsPages = new PagedContext<string>(publishedProviderIds, 100);
            List<string> filteredPublishedProviderIds = new List<string>();

            while (publishedProviderIdsPages.HasPages)
            {
                providerIdsParameter.Value = publishedProviderIdsPages.NextPage();

                IEnumerable<dynamic> results = await _repository.DynamicQuery(query);

                filteredPublishedProviderIds.AddRange(results.Select(_ => (string)_.publishedProviderId));
            }

            return filteredPublishedProviderIds;
        }

        public async Task<IEnumerable<string>> GetPublishedProviderErrorSummaries(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            List<string> results = new List<string>();

            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = @"
                              SELECT 
                                  DISTINCT e.summaryErrorMessage
                              FROM publishedProvider c
                              JOIN e IN c.content.current.errors
                              WHERE c.documentType = 'PublishedProvider'
                              AND c.content.current.specificationId = @specificationId
                              AND c.deleted = false
                              AND IS_NULL(c.content.current.errors) = false",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId),
                }
            };

            dynamic queryResults = (await _repository
                    .DynamicQuery(query));

            foreach (dynamic item in queryResults)
            {
                results.Add((string)item.summaryErrorMessage);
            }

            return results;
        }
        
        public async Task<IEnumerable<PublishedProviderFundingStreamStatus>> GetPublishedProviderStatusCounts(string specificationId,
            string providerType,
            string localAuthority,
            string status,
            bool? isIndicative = null,
            string monthYearOpened = null)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            List<PublishedProviderFundingStreamStatus> results = new List<PublishedProviderFundingStreamStatus>();

            StringBuilder additionalFilter = new StringBuilder();

            List<CosmosDbQueryParameter> cosmosDbQueryParameters = new List<CosmosDbQueryParameter> { new CosmosDbQueryParameter("@specificationId", specificationId) };

            if (providerType.IsNotNullOrWhitespace())
            {
                additionalFilter.Append($" and f.content.current.provider.providerType = @providerType ");
                cosmosDbQueryParameters.Add(new CosmosDbQueryParameter("@providerType", providerType));
            }

            if (localAuthority.IsNotNullOrWhitespace())
            {
                additionalFilter.Append($" and f.content.current.provider.localAuthorityName = @localAuthorityName ");
                cosmosDbQueryParameters.Add(new CosmosDbQueryParameter("@localAuthorityName", localAuthority));
            }

            if (status.IsNotNullOrWhitespace())
            {
                additionalFilter.Append($" and f.content.current.status = @status ");
                cosmosDbQueryParameters.Add(new CosmosDbQueryParameter("@status", status));
            }

            if (isIndicative.HasValue)
            {
                additionalFilter.Append(" and f.content.current.isIndicative = @isIndicative");
                cosmosDbQueryParameters.Add(new CosmosDbQueryParameter("@isIndicative", isIndicative.Value));
            }

            if (monthYearOpened.IsNotNullOrWhitespace())
            {
                DateTimeOffset openDateStart = DateTimeOffset.ParseExact($"1 {monthYearOpened}", "d MMMM yyyy", CultureInfo.InvariantCulture);
                DateTimeOffset openDateEnd = openDateStart.AddMonths(1);

                additionalFilter.Append(" and f.content.current.provider.dateOpened >= @openDateStart and f.content.current.provider.dateOpened < @openDateEnd");
                cosmosDbQueryParameters.Add(new CosmosDbQueryParameter("@openDateStart", openDateStart));
                cosmosDbQueryParameters.Add(new CosmosDbQueryParameter("@openDateEnd", openDateEnd));
            }

            CosmosDbQuery query = new CosmosDbQuery
            {
                QueryText = $@"SELECT COUNT(1) AS count, f.content.current.fundingStreamId, f.content.current.status, SUM(f.content.current.totalFunding) AS totalFundingSum
                                FROM f
                                where f.documentType = 'PublishedProvider' and f.content.current.specificationId = @specificationId and f.deleted = false {additionalFilter}
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
                    TotalFunding = (decimal?)item.totalFundingSum
                });
            }

            return results;
        }

        public ICosmosDbFeedIterator<PublishedProviderVersion> GetRefreshedProviderVersionBatchProcessing(string specificationId,
            int batchSize)
        {
            CosmosDbQuery query = CreateQueryForRefreshedProviderVersionBatchProcessing(specificationId);

            return _repository.GetFeedIterator<PublishedProviderVersion>(query, batchSize);
        }

        private static CosmosDbQuery CreateQueryForRefreshedProviderVersionBatchProcessing(string specificationId)
        {
            return new CosmosDbQuery
            {
                QueryText = @"SELECT 
                                    {
                                        'id':c.content.id,
                                        'variationReasons':c.content.variationReasons,
                                        'providerId':c.content.providerId,
                                        'status':c.content.status,
                                        'version':c.content.version,
                                        'provider' : {
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
                                        }
                                    } as content
                                FROM publishedProviders c
                                WHERE c.documentType = 'PublishedProviderVersion' 
                                AND c.content.specificationId = @specificationId 
                                AND (c.content.status = 'Updated' OR c.content.status = 'Released')
                                AND c.deleted = false
                                ORDER BY c.content.providerId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId)
                }
            };
        }

        public async Task<IEnumerable<(string Code, string Name)>> GetPublishedProviderFundingLines(string specificationId, GroupingReason fundingLineType)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            List<(string Code, string Name)> fundingLines = new List<(string Code, string Name)>();
            IEnumerable<dynamic> queryResults = await _repository
             .DynamicQuery(new CosmosDbQuery
             {
                 QueryText = @"
                                SELECT DISTINCT f.fundingLineCode, f.name
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
                fundingLines.Add((item.fundingLineCode, item.name));
            }

            return await Task.FromResult(fundingLines.DistinctBy(_ => _.Code));
        }

        public async Task<int> QueryPublishedFundingCount(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons)
        {
            CosmosDbQuery query = _publishedFundingQueryBuilder.BuildCountQuery(fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                variationReasons);

            IEnumerable<dynamic> dynamicQuery = await _repository.DynamicQuery(query);

            return Convert.ToInt32(dynamicQuery
                .Single());
        }

        public async Task<IEnumerable<PublishedFundingIndex>> QueryPublishedFunding(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons,
            int top,
            int? pageRef,
            int totalCount)
        {
            CosmosDbQuery query = _publishedFundingQueryBuilder.BuildQuery(fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                variationReasons,
                top,
                pageRef,
                totalCount);

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
                    StatusChangedDate = _.statusChangedDate,
                    VariationReasons = _.variationReasons
                });
        }

        public async Task<(string providerVersionId, string providerId)> GetPublishedProviderId(string publishedProviderVersion)
        {
            Guard.IsNullOrWhiteSpace(publishedProviderVersion, nameof(publishedProviderVersion));

            IEnumerable<dynamic> queryResults = await _repository
             .DynamicQuery(new CosmosDbQuery
             {
                 QueryText = @"
                                SELECT c.content.released.provider.providerVersionId, 
                                c.content.released.provider.providerId 
                                FROM c
                                WHERE c.documentType = 'PublishedProvider'
                                AND c.content.released.fundingId = @publishedProviderVersion",
                 Parameters = new[]
                 {
                    new CosmosDbQueryParameter("@publishedProviderVersion", publishedProviderVersion),
                 }
             });

            dynamic item = queryResults.SingleOrDefault();

            if (item == null)
            {
                return (providerVersionId: null, providerId: null);
            }

            return (providerVersionId: (dynamic)item.providerVersionId, providerId: (dynamic)item.providerId);
        }

        public async Task<IEnumerable<PublishedProviderFundingCsvData>> GetPublishedProvidersFundingDataForCsvReport(IEnumerable<string> publishedProviderIds, string specificationId, params PublishedProviderStatus[] statuses)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNotEmpty(publishedProviderIds, nameof(publishedProviderIds));
            Guard.Ensure(publishedProviderIds.Count() <= 100, "You can only filter against 100 published provider ids at a time");
            Guard.IsNotEmpty(statuses, nameof(statuses));

            StringBuilder queryTextBuilder = new StringBuilder(@"
                              SELECT 
                                    c.content.current.specificationId,
                                    c.content.current.fundingStreamId, 
                                    c.content.current.fundingPeriodId,
                                    c.content.current.status,
                                    c.content.current.provider.providerId,
                                    c.content.current.provider.ukprn,
                                    c.content.current.provider.urn,
                                    c.content.current.provider.upin,
                                    c.content.current.provider.name,
                                    c.content.current.totalFunding
                              FROM publishedProvider c
                              WHERE c.documentType = 'PublishedProvider'
                              AND c.deleted = false 
                              AND c.content.current.specificationId = @specificationId");

            List<CosmosDbQueryParameter> cosmosDbQueryParameters = new List<CosmosDbQueryParameter>{
                new CosmosDbQueryParameter("@specificationId", specificationId)
            };

            string publishedProviderIdQueryText = string.Join(',', publishedProviderIds.Select((_, index) => $"@publishedProviderId_{index}"));
            queryTextBuilder.Append($" AND c.content.current.publishedProviderId IN ({publishedProviderIdQueryText})");

            cosmosDbQueryParameters.AddRange(publishedProviderIds.Select((_, index) => new CosmosDbQueryParameter($"@publishedProviderId_{index}", _)));

            string statusesQueryText = string.Join(',', statuses.Select((_, index) => $"@status_{index}"));
            queryTextBuilder.Append($" AND c.content.current.status IN ({statusesQueryText})");

            cosmosDbQueryParameters.AddRange(statuses.Select((_, index) => new CosmosDbQueryParameter($"@status_{index}", _.ToString())));

            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = queryTextBuilder.ToString(),
                Parameters = cosmosDbQueryParameters
            };

            IEnumerable<dynamic> results = await _repository.DynamicQuery(cosmosDbQuery);

            return results.Select(_ => new PublishedProviderFundingCsvData()
            {
                SpecificationId = (string)_.specificationId,
                FundingStreamId = (string)_.fundingStreamId,
                FundingPeriodId = (string)_.fundingPeriodId,
                ProviderName = (string)_.name,
                Ukprn = (string)_.ukprn,
                Urn = (string)_.urn,
                Upin = (string)_.upin,
                TotalFunding = (decimal?)_.totalFunding,
                Status = (string)_.status
            });
        }

        public async Task DeletePublishedProviders(IEnumerable<PublishedProvider> publishedProviders)
        {
            Guard.IsNotEmpty(publishedProviders, nameof(publishedProviders));

            await _repository.BulkDeleteAsync(publishedProviders.Select(p => new KeyValuePair<string, PublishedProvider>(p.Id, p)), hardDelete: false);
        }
    }
}