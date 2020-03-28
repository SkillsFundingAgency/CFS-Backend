using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Nito.AsyncEx;

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

        public async Task<(bool Ok, string Message)> IsHealthOk()
        {
            try
            {
                Initialize();
                var container = _container.Value;
                return await Task.FromResult((true, string.Empty));
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
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
            CloudBlobContainer container = _container.Value;

            return container.GetBlockBlobReference(blobName);
        }

        public async Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName)
        {
            EnsureBlobClient();

            return await _container.Value.GetBlobReferenceFromServerAsync(blobName);
        }

        public async Task<bool> BlobExistsAsync(string blobName)
        {
            EnsureBlobClient();
            var blob = _container.Value.GetBlockBlobReference(blobName);

            return await blob.ExistsAsync(null, null);
        }

        public async Task<Stream> DownloadToStreamAsync(ICloudBlob blob)
        {
            var stream = new MemoryStream();
            await blob.DownloadToStreamAsync(stream);
            stream.Position = 0;

            return stream;
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

        public async Task<Stream> GetAsync(string blobName)
        {
            EnsureBlobClient();

            ICloudBlob blob = await _container.Value.GetBlobReferenceFromServerAsync(blobName);

            return await blob.OpenReadAsync(null, null, null);
        }

        public async Task UploadAsync(ICloudBlob blob, string data)
        {
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                await UploadAsync(blob, stream);
            }
        }

        public async Task UploadAsync(ICloudBlob blob, Stream data)
        {
            //reset to start to handle resilience policy retrying on a single stream instance
            data.Position = 0;
            
            await blob.UploadFromStreamAsync(data);
        }

        public async Task AddMetadataAsync(ICloudBlob blob, IDictionary<string, string> metadata)
        {
            foreach (var metadataItem in metadata.Where(_=>!string.IsNullOrEmpty(_.Value)))
            {
                blob.Metadata.Add(
                    ReplaceInvalidMetadataKeyCharacters(metadataItem.Key), 
                    metadataItem.Value);
            }

            await blob.SetMetadataAsync();
        }

        private string ReplaceInvalidMetadataKeyCharacters(string metadataKey)
        {
            return metadataKey.Replace('-', '_');
        }
    }
}
