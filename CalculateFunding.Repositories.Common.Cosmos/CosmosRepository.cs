using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CalculateFunding.Repositories.Common.Cosmos
{
    public class CosmosRepository
    {
        private readonly ILogger _logger;
        private readonly string _collectionName;
        private readonly string _partitionKey;
        private readonly string _databaseName;
        private readonly DocumentClient _documentClient;
        private readonly Uri _collectionUri;
        private ResourceResponse<DocumentCollection> _collection;

        public CosmosRepository(RepositorySettings settings, ILogger logger)
        {
            _logger = logger;
            _collectionName = settings.CollectionName;
            _partitionKey = settings.PartitionKey;
            _databaseName = settings.DatabaseName;
            _documentClient = DocumentDbConnectionString.Parse(settings.ConnectionString); ;
            _collectionUri = UriFactory.CreateDocumentCollectionUri(_databaseName, _collectionName);
        }

        public async Task EnsureCollectionExists()
        {
            if(_collection == null)
            {
                var collection = new DocumentCollection { Id = _collectionName };
                if (_partitionKey != null)
                {
                    collection.PartitionKey.Paths.Add(_partitionKey);
                }

                try
                {
                    await _documentClient.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(_databaseName));
                }
                catch (DocumentClientException e)
                {
                    await _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = _databaseName });
                }


                _collection = await _documentClient.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri(_databaseName), collection);
            }


        }



        public async Task SetThroughput(int requestUnits)
        {
            //Fetch the resource to be updated
            OfferV2 offer = (OfferV2) _documentClient.CreateOfferQuery()
                .Where(r => r.ResourceLink == _collection.Resource.SelfLink)
                .AsEnumerable()
                .SingleOrDefault();

            if(offer.Content.OfferThroughput != requestUnits)
            {
                // Set the throughput to the new value, for example 12,000 request units per second
                offer = new OfferV2(offer, requestUnits);

                //Now persist these changes to the database by replacing the original resource
                await _documentClient.ReplaceOfferAsync(offer);
            }
        }

        private static string GetDocumentType<T>()
        {
            return typeof(T).Name;
        }

        public IQueryable<DocumentEntity<T>> Read<T>(int maxItemCount = 1000) where T : IIdentifiable
        {
            // Set some common query options
            var queryOptions = new FeedOptions { MaxItemCount = maxItemCount };

            return _documentClient.CreateDocumentQuery<DocumentEntity<T>>(_collectionUri, queryOptions).Where(x => x.DocumentType == GetDocumentType<T>() && !x.Deleted);
        }

        public async Task<DocumentEntity<T>> ReadAsync<T>(string id) where T : IIdentifiable
        {
            // Here we find the Andersen family via its LastName
            var response = await Read<T>(maxItemCount: 1).Where(x => x.Id == id).AsDocumentQuery().ExecuteNextAsync< DocumentEntity<T>>();
            return response.FirstOrDefault();
        }

        public IQueryable<T> Query<T>(string directSql = null, int maxItemCount = -1) where T : IIdentifiable
        {
            // Set some common query options
            var queryOptions = new FeedOptions { MaxItemCount = maxItemCount };

            if (!string.IsNullOrEmpty(directSql))
            {
                // Here we find the Andersen family via its LastName
                return _documentClient.CreateDocumentQuery<DocumentEntity<T>>(_collectionUri,
                    directSql,
                    queryOptions).Select(x => x.Content).AsQueryable();
            }
            return _documentClient.CreateDocumentQuery<DocumentEntity<T>>(_collectionUri, queryOptions).Select(x => x.Content).AsQueryable();
        }

        public IEnumerable<string> QueryAsJson(string directSql = null, int maxItemCount = -1)
        {
            // Set some common query options
            var queryOptions = new FeedOptions { MaxItemCount = maxItemCount };

            
            IEnumerable<Document> documents = _documentClient.CreateDocumentQuery<Document>(_collectionUri, directSql, queryOptions).ToArray();
            foreach (var document in documents)
            {
                dynamic json = document;
                yield return JsonConvert.SerializeObject(json.Content); // haven't tried this yet!
            }
        }

        public async Task<HttpStatusCode> DeleteAsync<T>(string id) where T : IIdentifiable
        {
            var doc = await ReadAsync<T>(id);
            doc.Deleted = true;
            var response = await _documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseName, _collectionName, doc.Id), doc);
            return response.StatusCode;
        }


        public async Task<HttpStatusCode> CreateAsync<T>(T entity) where T : IIdentifiable
        {
            var doc = new DocumentEntity<T>(entity)
            {
                DocumentType = GetDocumentType<T>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var response = await _documentClient.UpsertDocumentAsync(_collectionUri, doc);
            return response.StatusCode;

        }

        public async Task BulkCreateAsync<T>(IList<T> entities, int degreeOfParallelism) where T : Reference
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            int taskCount;
            // set TaskCount = 10 for each 10k RUs, minimum 1, maximum 250
            taskCount = Math.Max(degreeOfParallelism, 1);
            taskCount = Math.Min(taskCount, 250);
            

            await Task.Run(() => Parallel.ForEach(entities, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism }, (item) =>
            {
                Task.WaitAll(CreateAsync(item));
            }));


            stopwatch.Stop();

            var itemsPerSec = entities.Count / (stopwatch.ElapsedMilliseconds / 1000M);
        }

        public async Task<HttpStatusCode> UpdateAsync<T>(T entity) where T : Reference
        {
            var documentType = GetDocumentType<T>();
            var doc = new DocumentEntity<T>(entity);
            if (doc.DocumentType != null && doc.DocumentType != documentType)
            {
                throw new ArgumentException($"Cannot change {entity.Id} from {doc.DocumentType} to {typeof(T).Name}");
            }
            doc.DocumentType = documentType; // in case not specified
            doc.UpdatedAt = DateTime.UtcNow;
            var response = await _documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseName, _collectionName, entity.Id), doc);
            return response.StatusCode;
        }

    }
}
