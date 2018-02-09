using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.AzureStorage
{
    public interface IBlobClient
    {
        string GetBlobSasUrl(string blobName, DateTimeOffset finish,
            SharedAccessBlobPermissions permissions);

        ICloudBlob GetBlockBlobReference(string blobName);

        Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName);

        void Initialize();
    }
}
