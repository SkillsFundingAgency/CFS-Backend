using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using Microsoft.Azure.Storage.Blob;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryBlobClient : IBlobClient
    {
        private readonly ConcurrentDictionary<string, string> _files;

        public InMemoryBlobClient()
        {
            _files = new ConcurrentDictionary<string, string>();
        }

        public Task<bool> BlobExistsAsync(string blobName, string containerName = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DoesBlobExistAsync(string blobName, string containerName = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> DownloadAsync<T>(string blobName, string containerName = null)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> DownloadToStreamAsync(ICloudBlob blob)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetAsync(string blobName, string containerName = null)
        {
            throw new NotImplementedException();
        }

        public Task BatchProcessBlobs(Func<IEnumerable<IListBlobItem>, Task> batchProcessor, string containerName = null, int batchSize = 50)
        {
            throw new NotImplementedException();
        }

        public Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName, string containerName = null)
        {
            throw new NotImplementedException();
        }

        public string GetBlobSasUrl(string blobName, DateTimeOffset finish, SharedAccessBlobPermissions permissions, string containerName = null)
        {
            throw new NotImplementedException();
        }

        public ICloudBlob GetBlockBlobReference(string blobName, string containerName = null)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public Task<(bool Ok, string Message)> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public Task<string> UploadFileAsync<T>(string blobName, T content, string containerName = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> UploadFileAsync(string blobName, string fileContents, string containerName = null)
        {
            Guard.IsNullOrWhiteSpace(blobName, nameof(blobName));

            _files[blobName] = fileContents;

//            File.WriteAllText($"c:\\dev\\output\\{blobName}.json", fileContents);

            return Task.FromResult(blobName);
        }

        public void VerifyFileName(string fileName)
        {
            throw new NotImplementedException();
        }

        public ConcurrentDictionary<string, string> GetFiles()
        {
            return _files;
        }

        public IEnumerable<IListBlobItem> ListBlobs(string prefix, string containerName = null, bool useFlatBlobListing = false, BlobListingDetails blobListingDetails = BlobListingDetails.None)
        {
            throw new NotImplementedException();
        }

        public Task UploadFileAsync(ICloudBlob blob, Stream data)
        {
            throw new NotImplementedException();
        }

        public Task AddMetadataAsync(ICloudBlob blob, IDictionary<string, string> metadata)
        {
            throw new NotImplementedException();
        }
    }
}
