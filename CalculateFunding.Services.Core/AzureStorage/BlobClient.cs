using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.AzureStorage
{
    public class BlobClient : IBlobClient
    {
        private Lazy<CloudBlobContainer> _container;
        private readonly AzureStorageSettings _azureStorageSettings;

        public BlobClient(AzureStorageSettings azureStorageSettings)
        {
            _azureStorageSettings = azureStorageSettings;

            Initialize();
        }

        public string GetBlobSasUrl(string blobName, DateTimeOffset start, DateTimeOffset finish, 
            SharedAccessBlobPermissions permissions)
        {
            ICloudBlob blob = GetBlockBlobReference(blobName);

            string sharedAccessSignature = GetSharedAccessSignature(blob, start, finish, permissions);

            return $"{blob.Uri}{sharedAccessSignature}";
        }

        public ICloudBlob GetBlockBlobReference(string blobName)
        {
            EnsureBlobClient();

            return _container.Value.GetBlockBlobReference(blobName);
        }

        public void Initialize()
        {
            _container = new Lazy<CloudBlobContainer>(() =>
            {
                var credentials = CloudStorageAccount.Parse(_azureStorageSettings.ConnectionString);
                var client = credentials.CreateCloudBlobClient();
                var container = client.GetContainerReference(_azureStorageSettings.ContainerName.ToLower());

                AsyncContext.Run(() => container.CreateIfNotExistsAsync());

                return container;
            });
        }

        void EnsureBlobClient()
        {
            if (_container == null)
                Initialize();
        }

        string GetSharedAccessSignature(ICloudBlob blob, DateTimeOffset start, DateTimeOffset finish, SharedAccessBlobPermissions permissions)
        {
            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = start,
                SharedAccessExpiryTime = finish,
                Permissions = permissions
            };

            return blob.GetSharedAccessSignature(sasConstraints);
        }
    }
}
