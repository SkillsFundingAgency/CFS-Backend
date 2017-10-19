using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Allocations.Models.Framework;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace Allocations.Respository
{
    public class Repository : IDisposable
    {
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly DocumentClient _documentClient;
        private readonly Uri _collectionUri;

        public Repository(string collectionName)
        {
            _collectionName = collectionName;
            _databaseName = ConfigurationManager.AppSettings["DocumentDB.DatabaseName"];

            var endpoint = new Uri(ConfigurationManager.AppSettings["DocumentDB.Endpoint"]);
            var key = ConfigurationManager.AppSettings["DocumentDB.Key"];

            


            _documentClient = new DocumentClient(endpoint, key);

            _collectionUri = UriFactory.CreateDocumentCollectionUri(_databaseName, _collectionName);

        }

        private async Task EnsureCollectionExists()
        {
            await _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = _databaseName });
            await _documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(_databaseName), new DocumentCollection { Id = _collectionName });

        }

        public async Task UpsertAsync<T>(T payload)
        {
            await EnsureCollectionExists();
            var response = await _documentClient.UpsertDocumentAsync(_collectionUri, payload);
        }

        private class _ProviderDataset
        {
            public string URN { get; set; }
            public string ModelName { get; set; }
        }
        public string[] GetProviderUrns(string modelName)
        {
            // Set some common query options
            var queryOptions = new FeedOptions { MaxItemCount = -1 };

            return _documentClient.CreateDocumentQuery<_ProviderDataset>(_collectionUri,
                queryOptions).Where(x =>  x.ModelName == modelName).Select(x => x.URN).ToArray();

        }

        public class ProviderDatasetResult
        {
            public ProviderDatasetResult(string datasetName, string json)
            {
                DatasetName = datasetName;
                Json = json;
            }

            public string DatasetName { get;  }
            public string Json { get; }
        }

        public IEnumerable<ProviderDatasetResult> GetProviderDatasets(string modelName, string urn)
        {
            // Set some common query options
            var queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Now execute the same query via direct SQL
            var datasets = _documentClient.CreateDocumentQuery<object>(_collectionUri,
                $"SELECT * FROM T WHERE T.ModelName = '{modelName}' AND T.URN = '{urn}'",
                queryOptions);

            foreach (object dataset in datasets.ToArray())
            {
                dynamic d = dataset;
                yield return new ProviderDatasetResult(d.DatasetName, JsonConvert.SerializeObject(dataset));
            }
        }

        public void Dispose()
        {
            _documentClient?.Dispose();
        }

    }
}
