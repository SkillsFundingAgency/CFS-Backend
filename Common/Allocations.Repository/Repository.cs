using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;

namespace Allocations.Repository
{
    public class Repository<T> : IDisposable where T : DocumentEntity
    {
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly DocumentClient _documentClient;
        private readonly Uri _collectionUri;
        private readonly string _documentType = typeof(T).Name;

        public Repository(Uri endpoint, string key, string databaseName, string collectionName)
        {
            _collectionName = collectionName;
            _databaseName = databaseName;



            _documentClient = new DocumentClient(endpoint, key);

            _collectionUri = UriFactory.CreateDocumentCollectionUri(_databaseName, _collectionName);

        }

        private async Task EnsureCollectionExists()
        {
            await _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = _databaseName });
            await _documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(_databaseName), new DocumentCollection { Id = _collectionName });

        }

        public async Task UpsertAsync(T payload)
        {
            await EnsureCollectionExists();
            var response = await _documentClient.UpsertDocumentAsync(_collectionUri, payload);
        }

        public IQueryable<T> Read(int maxItemCount = 1000)
        {

            // Set some common query options
            var queryOptions = new FeedOptions { MaxItemCount = maxItemCount };

            return _documentClient.CreateDocumentQuery<T>(_collectionUri, queryOptions).Where(x => x.DocumentType == _documentType && !x.Deleted);
        }

        public async Task<T> ReadAsync(string id)
        {
            // Here we find the Andersen family via its LastName
            var response = await Read(maxItemCount: 1).Where(x => x.Id == id).AsDocumentQuery().ExecuteNextAsync<T>();
            return response.FirstOrDefault();
        }

        public async Task<TSpecific> ReadAsync<TSpecific>(string id) where TSpecific : T
        {
            var response = await _documentClient.CreateDocumentQuery<TSpecific>(_collectionUri, new FeedOptions { MaxItemCount = 1 }).Where(x => x.Id == id && x.DocumentType == _documentType && !x.Deleted).AsDocumentQuery().ExecuteNextAsync<TSpecific>(); ;
            return response.FirstOrDefault();
        }

        public IQueryable<T> Query(string directSql = null, int maxItemCount = -1)
        {
            // Set some common query options
            var queryOptions = new FeedOptions { MaxItemCount = maxItemCount };

            if (!string.IsNullOrEmpty(directSql))
            {
                // Here we find the Andersen family via its LastName
                return _documentClient.CreateDocumentQuery<T>(_collectionUri,
                    directSql,
                    queryOptions);
            }
            return _documentClient.CreateDocumentQuery<T>(_collectionUri, queryOptions);

        }

        public IEnumerable<string> QueryAsJson(string directSql = null, int maxItemCount = -1)
        {
            // Set some common query options
            var queryOptions = new FeedOptions { MaxItemCount = maxItemCount };

            
            IEnumerable<Document> documents = _documentClient.CreateDocumentQuery<Document>(_collectionUri, directSql, queryOptions).ToArray();
            foreach (var document in documents)
            {
                dynamic json = document;
                yield return JsonConvert.SerializeObject(json);
            }
        }

        public async Task<HttpStatusCode> DeleteAsync(string id)
        {
            var entity = await ReadAsync(id);
            entity.Deleted = true;
            return await UpdateAsync(entity);
        }


        public async Task<HttpStatusCode> CreateAsync<TAny>(TAny entity) where TAny : T
        {
            await EnsureCollectionExists();
            entity.DocumentType = _documentType; // in case not specified
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            var response = await _documentClient.UpsertDocumentAsync(_collectionUri, entity);
            return response.StatusCode;
        }

        public async Task<HttpStatusCode> UpdateAsync<TAny>(TAny entity) where TAny : T
        {
            if (entity.DocumentType != null && entity.DocumentType != _documentType)
            {
                throw new ArgumentException($"Cannot change {entity.Id} from {entity.DocumentType} to {typeof(T).Name}");
            }
            entity.DocumentType = _documentType; // in case not specified
            entity.UpdatedAt = DateTime.UtcNow;
            var response = await _documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseName, _collectionName, entity.Id), entity);
            return response.StatusCode;
        }



        public void Dispose()
        {
            _documentClient?.Dispose();
        }

    }
}
