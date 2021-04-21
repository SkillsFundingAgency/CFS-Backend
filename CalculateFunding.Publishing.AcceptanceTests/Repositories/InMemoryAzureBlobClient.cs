using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using Microsoft.Azure.Storage.Blob;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryAzureBlobClient : IBlobClient
    {
        private readonly ConcurrentDictionary<string, string> _files;

        public InMemoryAzureBlobClient()
        {
            _files = new ConcurrentDictionary<string, string>();
        }

        public Task<bool> BlobExistsAsync(string blobName)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> DownloadToStreamAsync(ICloudBlob blob)
        {
            throw new NotImplementedException();
        }

        public Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName)
        {
            throw new NotImplementedException();
        }

        public string GetBlobSasUrl(string blobName, DateTimeOffset finish, SharedAccessBlobPermissions permissions)
        {
            throw new NotImplementedException();
        }

        public ICloudBlob GetBlockBlobReference(string blobName)
        {
            _files.TryAdd(blobName, string.Empty);

            return new CloudBlobInMemory(blobName);
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public Task<(bool Ok, string Message)> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public async Task UploadAsync(ICloudBlob blob, string data)
        {
            _files[blob.Name] = data;

            //File.WriteAllText($"c:\\dev\\output\\{blob.Name}.json", data);

            await Task.CompletedTask;
        }

        public Task UploadAsync(ICloudBlob blob, Stream data)
        {
            throw new NotImplementedException();
        }

        public Task<string> UploadFileAsync(string blobName, string fileContents)
        {
            Guard.IsNullOrWhiteSpace(blobName, nameof(blobName));

            _files[blobName] = fileContents;

            //            File.WriteAllText($"c:\\dev\\output\\{blobName}.json", fileContents);

            return Task.FromResult(blobName);
        }

        public ConcurrentDictionary<string, string> GetFiles()
        {
            return _files;
        }

        public Task AddMetadataAsync(ICloudBlob blob, IDictionary<string, string> metadata)
        {
            foreach (KeyValuePair<string, string> metadataItem
                in metadata.Where(_ => !string.IsNullOrEmpty(_.Value)))
            {
                blob.Metadata.Add(metadataItem.Key, metadataItem.Value);
            }

            return Task.FromResult(true);
        }

        public async Task<ICloudBlob> CopyBlobAsync(string sourcePath, string destinationPath)
        {
            return new CloudBlobInMemory(destinationPath);
        }
    }
}
