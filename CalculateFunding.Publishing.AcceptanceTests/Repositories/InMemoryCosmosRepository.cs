using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using Microsoft.Azure.Cosmos;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryCosmosRepository : ICosmosRepository
    {
        public ConcurrentDictionary<string, ConcurrentDictionary<string, PublishedFundingVersion>> PublishedFundingVersions { get; private set; }

        public ConcurrentDictionary<string, ConcurrentDictionary<string, PublishedProviderVersion>> PublishedProviderVersions { get; private set; }

        public ConcurrentDictionary<string, ConcurrentBag<PublishedProvider>> PublishedProviders { get; private set; }

        // Keyed on SpecificationId
        public ConcurrentDictionary<string, ConcurrentBag<PublishedFunding>> PublishedFunding { get; private set; }

        public InMemoryCosmosRepository()
        {
            PublishedFundingVersions = new ConcurrentDictionary<string, ConcurrentDictionary<string, PublishedFundingVersion>>();
            PublishedProviders = new ConcurrentDictionary<string, ConcurrentBag<PublishedProvider>>();
            PublishedFunding = new ConcurrentDictionary<string, ConcurrentBag<PublishedFunding>>();
            PublishedProviderVersions = new ConcurrentDictionary<string, ConcurrentDictionary<string, PublishedProviderVersion>>();
        }

        public Task<IEnumerable<dynamic>> DynamicQueryPartitionedEntity<dynamic>(CosmosDbQuery cosmosDbQuery, string partitionEntityId = null)
        {
            if (cosmosDbQuery.Parameters.Count() == 1)
            {
                if (string.Equals(cosmosDbQuery.Parameters.First().Value?.ToString(), "PublishedFundingVersion", StringComparison.InvariantCultureIgnoreCase))
                {
                    var existingVersions = PublishedFundingVersions.Values
                        .SelectMany(c => c.Values)
                        .Where(c => c.EntityId == partitionEntityId);

                    if (existingVersions.AnyWithNullCheck())
                    {
                        int maxVersion = existingVersions
                            .Select(p => p.Version)
                            .Max();
                        IEnumerable<dynamic> result = new[] { maxVersion } as IEnumerable<dynamic>;

                        return Task.FromResult(result);
                    }
                }
            }

            return Task.FromResult(new dynamic[] { }.AsEnumerable());
        }

        public Task<HttpStatusCode> UpsertAsync<T>(T entity, string partitionKey = null, bool undelete = false, bool maintainCreatedDate = true) where T : IIdentifiable
        {
            if (typeof(T).Name == "PublishedProviderVersion")
            {
                PublishedProviderVersion publishedProviderVersion = entity as PublishedProviderVersion;
                if (!PublishedProviderVersions.ContainsKey(publishedProviderVersion.SpecificationId))
                {
                    PublishedProviderVersions.TryAdd(publishedProviderVersion.SpecificationId, new ConcurrentDictionary<string, PublishedProviderVersion>());
                }

                PublishedProviderVersions[publishedProviderVersion.SpecificationId][publishedProviderVersion.Id] = publishedProviderVersion;
                return Task.FromResult(HttpStatusCode.OK);
            }
            else if (typeof(T).Name == "PublishedFundingVersion")
            {
                PublishedFundingVersion publishedFundingVersion = entity as PublishedFundingVersion;
                if (!PublishedFundingVersions.ContainsKey(publishedFundingVersion.SpecificationId))
                {
                    PublishedFundingVersions.TryAdd(publishedFundingVersion.SpecificationId, new ConcurrentDictionary<string, PublishedFundingVersion>());
                }

                PublishedFundingVersions[publishedFundingVersion.SpecificationId][publishedFundingVersion.Id] = publishedFundingVersion;
                return Task.FromResult(HttpStatusCode.OK);
            }

            return Task.FromResult(HttpStatusCode.BadRequest);
        }

        public (bool Ok, string Message) IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public Task EnsureContainerExists()
        {
            throw new NotImplementedException();
        }

        public Task<ThroughputResponse> SetThroughput(int requestUnits)
        {
            throw new NotImplementedException();
        }

        public Task<int?> GetThroughput()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DocumentEntity<T>>> Read<T>(int itemsPerPage = 1000) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<T> ReadByIdAsync<T>(string id) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<T> ReadByIdPartitionedAsync<T>(string id, string partitionKey) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> Query<T>(Expression<Func<DocumentEntity<T>, bool>> query = null, int itemsPerPage = -1, int? maxItemCount = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> QueryPartitionedEntity<T>(CosmosDbQuery cosmosDbQuery, int itemsPerPage = -1, int? maxItemCount = null, string partitionKey = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> QuerySql<T>(CosmosDbQuery cosmosDbQuery, int itemsPerPage = -1, int? maxItemCount = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<dynamic>> DynamicQuery(CosmosDbQuery cosmosDbQuery)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<dynamic>> DynamicQuery(CosmosDbQuery cosmosDbQuery, int itemsPerPage = 1000)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> RawQuery<T>(CosmosDbQuery cosmosDbQuery, int itemsPerPage = -1, int? maxItemCount = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DocumentEntity<T>>> GetAllDocumentsAsync<T>(int itemsPerPage = 1000, Expression<Func<DocumentEntity<T>, bool>> query = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DocumentEntity<T>>> GetAllDocumentsAsync<T>(CosmosDbQuery cosmosDbQuery, int itemsPerPage = 1000) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task DocumentsBatchProcessingAsync<T>(Func<List<DocumentEntity<T>>, Task> persistBatchToIndex, int itemsPerPage = 1000, Expression<Func<DocumentEntity<T>, bool>> query = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task DocumentsBatchProcessingAsync<T>(Func<List<T>, Task> persistBatchToIndex, CosmosDbQuery cosmosDbQuery, int itemsPerPage = 1000) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DocumentEntity<T>>> QueryDocuments<T>(int itemsPerPage = -1, int? maxItemCount = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> QueryAsJson(int itemsPerPage = -1, int? maxItemCount = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> QueryAsJsonAsync(CosmosDbQuery cosmosDbQuery, int itemsPerPage = -1, int? maxItemCount = null)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> CreateAsync<T>(T entity, string partitionKey = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<DocumentEntity<T>> CreateDocumentAsync<T>(T entity, string partitionKey = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> CreateAsync<T>(KeyValuePair<string, T> entity) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<ItemResponse<DocumentEntity<T>>> CreateWithResponseAsync<T>(T entity) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task BulkCreateAsync<T>(IList<T> entities, int degreeOfParallelism = 5) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task BulkCreateAsync<T>(IEnumerable<KeyValuePair<string, T>> entities, int degreeOfParallelism = 5) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task BulkDeleteAsync<T>(IEnumerable<KeyValuePair<string, T>> entities, int degreeOfParallelism = 5, bool hardDelete = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task BulkUpsertAsync<T>(IList<T> entities, int degreeOfParallelism = 5, bool maintainCreatedDate = true, bool undelete = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task BulkUpsertAsync<T>(IEnumerable<KeyValuePair<string, T>> entities, int degreeOfParallelism = 5, bool maintainCreatedDate = true, bool undelete = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> UpdateAsync<T>(T entity, bool undelete = false) where T : Reference
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> BulkUpdateAsync<T>(IEnumerable<T> entities, string storedProcedureName) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public ICosmosDbFeedIterator<T> GetFeedIterator<T>(CosmosDbQuery cosmosDbQuery, int itemsPerPage = -1, int? maxItemCount = null) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task BulkSetContentsToNull<T>(IEnumerable<KeyValuePair<string, string>> identifiers, int degreeOfParallelism = 5) where T : class, IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> SetContentsToNull<T>(string id, string partitionKey) where T : class, IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<DocumentEntity<T>> ReadDocumentByIdAsync<T>(string id) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<DocumentEntity<T>> ReadDocumentByIdPartitionedAsync<T>(string id, string partitionKey) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> DeleteAsync<T>(string id, string partitionKey, bool hardDelete = false) where T : IIdentifiable
        {
            throw new NotImplementedException();
        }
    }
}
