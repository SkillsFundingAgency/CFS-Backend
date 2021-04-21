using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace CalculateFunding.Services.Core.Interfaces.AzureStorage
{
    public interface IBlobClient
    {
        Task<(bool Ok, string Message)> IsHealthOk();

        string GetBlobSasUrl(string blobName, DateTimeOffset finish,
            SharedAccessBlobPermissions permissions);

        ICloudBlob GetBlockBlobReference(string blobName);

        Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName);

        Task<bool> BlobExistsAsync(string blobName);

        Task<Stream> DownloadToStreamAsync(ICloudBlob blob);

        void Initialize();

        Task UploadAsync(ICloudBlob blob, string data);
        
        Task UploadAsync(ICloudBlob blob, Stream data);

        Task AddMetadataAsync(ICloudBlob blob, IDictionary<string, string> metadata);

        Task<ICloudBlob> CopyBlobAsync(string sourcePath, string destinationPath);
    }
}
