﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CalculateFunding.Repositories.Common.Cosmos
{
    public class Repository<T>  where T : Reference
    {
        private readonly ILogger _logger;
        private readonly string _collectionName;
        private readonly string _partitionKey;
        private readonly string _databaseName;
        private readonly DocumentClient _documentClient;
        private readonly Uri _collectionUri;
        private readonly string _documentType = typeof(T).Name;
        private ResourceResponse<DocumentCollection> _collection;

        public Repository(RepositorySettings settings, ILogger logger)
        {
            _logger = logger;
            var databaseName = settings.DatabaseName;

            _collectionName = settings.CollectionName;
            _partitionKey = settings.PartitionKey;
            _databaseName = databaseName;
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

                await _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = _databaseName });
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

        public IQueryable<DocumentEntity<T>> Read(int maxItemCount = 1000)
        {
            // Set some common query options
            var queryOptions = new FeedOptions { MaxItemCount = maxItemCount };

            return _documentClient.CreateDocumentQuery<DocumentEntity<T>>(_collectionUri, queryOptions).Where(x => x.DocumentType == _documentType && !x.Deleted);
        }

        public async Task<DocumentEntity<T>> ReadAsync(string id)
        {
            // Here we find the Andersen family via its LastName
            var response = await Read(maxItemCount: 1).Where(x => x.Id == id).AsDocumentQuery().ExecuteNextAsync< DocumentEntity<T>>();
            return response.FirstOrDefault();
        }

        public async Task<DocumentEntity<TSpecific>> ReadAsync<TSpecific>(string id) where TSpecific : T
        {
            var response = await _documentClient.CreateDocumentQuery<DocumentEntity<TSpecific>>(_collectionUri, new FeedOptions { MaxItemCount = 1 }).Where(x => x.Id == id && x.DocumentType == _documentType && !x.Deleted).AsDocumentQuery().ExecuteNextAsync<DocumentEntity<TSpecific>>(); ;
            return response.FirstOrDefault();
        }

        public IQueryable<T> Query(string directSql = null, int maxItemCount = -1)
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

        public async Task<HttpStatusCode> DeleteAsync(string id)
        {
            var doc = await ReadAsync(id);
            doc.Deleted = true;
            var response = await _documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseName, _collectionName, doc.Id), doc);
            return response.StatusCode;
        }


        public async Task<HttpStatusCode> CreateAsync<TAny>(TAny entity) where TAny : T
        {
            var doc = new DocumentEntity<TAny>(entity)
            {
                DocumentType = _documentType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var response = await _documentClient.UpsertDocumentAsync(_collectionUri, doc);
            return response.StatusCode;
        }

        public async Task BulkCreateAsync<TAny>(IList<TAny> entities, int degreeOfParallelism) where TAny : T
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

        public async Task<HttpStatusCode> UpdateAsync<TAny>(TAny entity) where TAny : T
        {
            var doc = new DocumentEntity<TAny>(entity);
            if (doc.DocumentType != null && doc.DocumentType != _documentType)
            {
                throw new ArgumentException($"Cannot change {entity.Id} from {doc.DocumentType} to {typeof(T).Name}");
            }
            doc.DocumentType = _documentType; // in case not specified
            doc.UpdatedAt = DateTime.UtcNow;
            var response = await _documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseName, _collectionName, entity.Id), doc);
            return response.StatusCode;
        }

    }
}
