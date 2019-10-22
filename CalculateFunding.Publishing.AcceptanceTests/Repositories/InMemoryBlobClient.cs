using System;
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
        Dictionary<string, string> _files = new Dictionary<string, string>();

        public Task<bool> BlobExistsAsync(string blobName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DoesBlobExistAsync(string blobName)
        {
            throw new NotImplementedException();
        }

        public Task<T> DownloadAsync<T>(string blobName)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> DownloadToStreamAsync(ICloudBlob blob)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetAsync(string blobName)
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

        public Task<string> UploadFileAsync<T>(string blobName, T contents)
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

        public void VerifyFileName(string fileName)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> GetFiles()
        {
            return _files;
        }
    }
}
