using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.IntegrationTests.Common.Data
{
    public abstract class CosmosBulkDataContext : JsonDataSource<CosmosIdentity>
    {
        private readonly CosmosClient _cosmosClient;

        protected Container CosmosContainer;

        protected CosmosBulkDataContext(IConfiguration configuration,
            string cosmosCollectionName,
            string templateResourceName,
            Assembly resourceAssembly)
            : base(templateResourceName,
                resourceAssembly)
        {
            Guard.ArgumentNotNull(configuration, nameof(configuration));
            Guard.IsNullOrWhiteSpace(cosmosCollectionName, nameof(cosmosCollectionName));

            IConfigurationSection cosmosConfiguration = configuration.GetSection("CosmosDbSettings");

            Guard.ArgumentNotNull(cosmosConfiguration, nameof(cosmosConfiguration));

            string cosmosDatabaseName = cosmosConfiguration["DatabaseName"];

            CosmosConnectionString connectionString = cosmosConfiguration["ConnectionString"];

            string cosmosUri = connectionString.Uri;
            string cosmosAuthKey = connectionString.AuthKey;

            Guard.IsNullOrWhiteSpace(cosmosDatabaseName, nameof(cosmosDatabaseName));
            Guard.IsNullOrWhiteSpace(cosmosUri, nameof(cosmosUri));
            Guard.IsNullOrWhiteSpace(cosmosAuthKey, nameof(cosmosAuthKey));

            _cosmosClient = new CosmosClient(cosmosUri,
                cosmosAuthKey,
                new CosmosClientOptions
                {
                    AllowBulkExecution = true
                });
            CosmosContainer = _cosmosClient.GetDatabase(cosmosDatabaseName)
                .GetContainer(cosmosCollectionName);
        }

        protected override void CreateImportStream(JsonDocument jsonDocument,
            List<CosmosIdentity> batchIdentities,
            List<ImportStream> temporaryDocuments,
            string document)
        {
            string documentId = GetDocumentId(jsonDocument);
            string partitionKey = GetPartitionKey(jsonDocument);

            CosmosIdentity cosmosIdentity = new CosmosIdentity(documentId, partitionKey);

            ImportedDocuments.Add(cosmosIdentity);
            batchIdentities.Add(cosmosIdentity);
            temporaryDocuments.Add(ImportStream.ForCosmos(new MemoryStream(document.AsUTF8Bytes()), documentId, partitionKey));
        }

        protected override void PerformExtraCleanUp()
        {
            RemoveContextData()
                .Wait();

            _cosmosClient?.Dispose();
        }

        private string GetDocumentId(JsonDocument document) =>
            document.RootElement.TryGetProperty("id", out JsonElement id)
                ? id.GetString()
                : throw new InvalidOperationException("Didn't locate Id property on json document");

        protected override void RunImportTask(ImportStream importStream)
        {
            MemoryStream cosmosStream = new MemoryStream((int) importStream.Stream.Length);
            importStream.Stream.CopyToAsync(cosmosStream)
                .Wait();
            importStream.Stream.Position = 0;

            PartitionKey partitionKey = importStream.PartitionKey;

            ResponseMessage cosmosResponse = CosmosContainer.CreateItemStreamAsync(cosmosStream, partitionKey)
                .GetAwaiter()
                .GetResult();

            bool requestSucceeded = cosmosResponse.IsSuccessStatusCode;
            
            string documentId = importStream.Id;
            
            string failedMessage = $"Failed to insert document {documentId}";

            TraceInformation(requestSucceeded
                ? $"Inserted document {documentId}"
                : failedMessage);

            ThrowExceptionIfRequestFailed(requestSucceeded, failedMessage);
        }

        protected override void RunRemoveTask(CosmosIdentity cosmosIdentity)
        {
            string id = cosmosIdentity.Id;

            ResponseMessage cosmosResponse = CosmosContainer.DeleteItemStreamAsync(id,
                  cosmosIdentity.PartitionKey != null ? new PartitionKey(cosmosIdentity.PartitionKey) : PartitionKey.None)
                .GetAwaiter()
                .GetResult();

            if (cosmosResponse.StatusCode == HttpStatusCode.NotFound)
            {
                TraceInformation($"Did not locate document with Id {id} to delete in collection");
                
                return;
            }

            TraceInformation(cosmosResponse.IsSuccessStatusCode
                ? $"Deleted cosmos document {id}"
                : $"Failed to delete cosmos document {id}");
        }

        public async Task<TEntity> GetById<TEntity>(string id,
            string partitionKey)
            where TEntity : class, IIdentifiable
        {
            try
            {
                DocumentEntity<TEntity> response = await CosmosContainer.ReadItemAsync<DocumentEntity<TEntity>>(id, new PartitionKey(partitionKey));

                return response.Content;
            }
            catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private class CosmosConnectionString
        {
            private CosmosConnectionString(string connectionString)
            {
                Guard.IsNullOrWhiteSpace(connectionString, nameof(connectionString));

                string[] parts = connectionString.Split(";");

                Uri = GetSetting(parts, "AccountEndpoint");
                AuthKey = GetSetting(parts, "AccountKey");
            }

            private string GetSetting(string[] parts,
                string key)
                => parts.First(_ => _.StartsWith(key))
                    .Replace($"{key}=", "");

            public string Uri { get; }

            public string AuthKey { get; }

            public static implicit operator CosmosConnectionString(string connectionString)
                => new CosmosConnectionString(connectionString);
        }
    }
}