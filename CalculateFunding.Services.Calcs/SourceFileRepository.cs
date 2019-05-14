using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class SourceFileRepository : BlobClient, ISourceFileRepository
    {
        private const string dllFileName = "implementation.dll";

        public SourceFileRepository(BlobStorageOptions blobStorageOptions) : base(blobStorageOptions) { }

        public async Task SaveAssembly(byte[] assemblyBytes, string specificationId)
        {
            string blobName = $"{specificationId}/{dllFileName}";

            ICloudBlob blockBlob = GetBlockBlobReference(blobName);

            using (MemoryStream memoryStream = new MemoryStream(assemblyBytes))
            {
                await blockBlob.UploadFromStreamAsync(memoryStream);
            }
        }

        public async Task<Stream> GetAssembly(string specificationId)
        {
            string blobName = $"{specificationId}/{dllFileName}";

            ICloudBlob blockBlob = GetBlockBlobReference(blobName);

            return await DownloadToStreamAsync(blockBlob);
        }

        public async Task SaveSourceFiles(byte[] zippedContent, string specificationId, string sourceType)
        {
            ICloudBlob blockBlob = GetBlockBlobReference($"{specificationId}/{specificationId}-{sourceType}.zip");

            using (MemoryStream memoryStream = new MemoryStream(zippedContent))
            {
                await blockBlob.UploadFromStreamAsync(memoryStream);
            }
        }

        public async Task<bool> DoesAssemblyExist(string specificationId)
        {
            string blobName = $"{specificationId}/{dllFileName}";

            ICloudBlob blockBlob = GetBlockBlobReference(blobName);

            return await blockBlob.ExistsAsync();
        }

        public async Task<bool> DeleteAssembly(string specificationId)
        {
            string blobName = $"{specificationId}/{dllFileName}";

            ICloudBlob blockBlob = GetBlockBlobReference(blobName);

            return await blockBlob.DeleteIfExistsAsync();
        }
    }
}
