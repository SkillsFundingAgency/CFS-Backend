using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CalculateFunding.Common.Utility;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.IntegrationTests.Common.Data
{
    public abstract class BlobBulkDataContext : DataSource<BlobIdentity>
    {
        protected BlobContainerClient BlobContainerClient;

        protected BlobBulkDataContext(IConfiguration configuration,
            string blobContainerName,
            string templateResourceName,
            Assembly resourceAssembly)
            : base(templateResourceName,
                resourceAssembly)
        {
            Guard.ArgumentNotNull(configuration, nameof(configuration));
            Guard.IsNullOrWhiteSpace(blobContainerName, nameof(blobContainerName));

            IConfigurationSection storageConfiguration = configuration.GetSection("CommonStorageSettings");
            Guard.ArgumentNotNull(storageConfiguration, nameof(storageConfiguration));
            
            string connectionString = storageConfiguration["ConnectionString"];
            Guard.IsNullOrWhiteSpace(connectionString, nameof(connectionString));

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            BlobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
        }

        protected override void CreateImportStream(JsonDocument jsonDocument,
            List<BlobIdentity> batchIdentities,
            List<ImportStream> temporaryDocuments,
            string document)
        {
            string blobName = GetBlobName(jsonDocument);

            BlobIdentity blobIdentity = new BlobIdentity(blobName);

            ImportedDocuments.Add(blobIdentity);
            batchIdentities.Add(blobIdentity);
            temporaryDocuments.Add(ImportStream.ForBlob(new MemoryStream(document.AsUTF8Bytes()), blobName));
        }

        protected override void PerformExtraCleanUp()
        {
            RemoveContextData()
                .Wait();
        }

        protected virtual string GetBlobName(JsonDocument document) => "N/A";

        protected override void RunImportTask(ImportStream importStream)
        {
            MemoryStream cosmosStream = new MemoryStream((int) importStream.Stream.Length);
            importStream.Stream.CopyToAsync(cosmosStream)
                .Wait();
            importStream.Stream.Position = 0;

            string blobName = importStream.Id;

            BlobContentInfo blobInfo = BlobContainerClient.UploadBlobAsync(blobName, importStream.Stream)
                .GetAwaiter()
                .GetResult();

            importStream.Stream.Dispose();

            string failedMessage = $"Failed to insert json document to blob store for {blobName}";

            bool requestSucceeded = blobInfo != null;

            TraceInformation(requestSucceeded
                ? $"Inserted json document to blob store for {blobName}"
                : failedMessage);

            ThrowExceptionIfRequestFailed(requestSucceeded, failedMessage);
        }

        protected override void RunRemoveTask(BlobIdentity documentIdentity)
        {
            string blobName = documentIdentity.Name;

            try
            {
                Response<bool> blobResponse = BlobContainerClient.DeleteBlobIfExistsAsync(blobName)
                .GetAwaiter()
                .GetResult();

                bool requestSucceeded = blobResponse.Value;
                TraceInformation(requestSucceeded? $"Deleted blob store json document {blobName}" : $"Blob {blobName} not exists.");
            }
            catch (Exception ex)
            {
                string failedMessage = $"Failed to delete blob store json document {blobName}. {ex.Message}";
                TraceInformation(failedMessage);
                ThrowExceptionIfRequestFailed(false, failedMessage);
            }
        }
    }
}