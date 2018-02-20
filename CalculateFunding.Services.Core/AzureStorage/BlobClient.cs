using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.IO;
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

        public string GetBlobSasUrl(string blobName, DateTimeOffset finish, 
            SharedAccessBlobPermissions permissions)
        {
            ICloudBlob blob = GetBlockBlobReference(blobName);

            string sharedAccessSignature = GetSharedAccessSignature(blob, finish, permissions);

            return $"{blob.Uri}{sharedAccessSignature}";
        }

        public ICloudBlob GetBlockBlobReference(string blobName)
        {
            EnsureBlobClient();

            return _container.Value.GetBlockBlobReference(blobName);
        }

        public Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName)
        {
            EnsureBlobClient();

            return _container.Value.GetBlobReferenceFromServerAsync(blobName);
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

        string GetSharedAccessSignature(ICloudBlob blob, DateTimeOffset finish, SharedAccessBlobPermissions permissions)
        {
            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = finish,
                Permissions = permissions
            };

            return blob.GetSharedAccessSignature(sasConstraints);
        }

	    async public Task<Stream> GetAsync(string blobName)
	    {
		    EnsureBlobClient();

		    var blob = await _container.Value.GetBlobReferenceFromServerAsync(blobName);

		    return await blob.OpenReadAsync(null, null, null);
	    }
	}
}
