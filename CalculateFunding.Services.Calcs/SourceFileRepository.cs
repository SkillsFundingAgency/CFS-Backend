using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.Azure.Storage.Blob;

namespace CalculateFunding.Services.Calcs
{
    public class SourceFileRepository : BlobClient, ISourceFileRepository
    {
        private const string dllFileName = "implementation.dll";

        public SourceFileRepository(
            BlobStorageOptions blobStorageOptions, 
            IBlobContainerRepository blobContainerRepository) 
            : base(blobStorageOptions, blobContainerRepository) { }

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
            string blobName = GetAssemblyBlobName(specificationId);

            ICloudBlob blockBlob = GetBlockBlobReference(blobName);

            return await DownloadToStreamAsync(blockBlob);
        }

        public string GetAssemblyETag(string specificationId)
        {
            string blobName = GetAssemblyBlobName(specificationId);

            ICloudBlob blockBlob = GetBlockBlobReference(blobName);

            blockBlob?.FetchAttributes();

            return blockBlob?.Properties?.ETag;
        }

        public async Task SaveSourceFiles(byte[] zippedContent, string specificationId, string sourceType)
        {
            ICloudBlob blockBlob = GetBlockBlobReference($"{specificationId}/{specificationId}-{sourceType}.zip");

            await using MemoryStream memoryStream = new MemoryStream(zippedContent);
            
            await blockBlob.UploadFromStreamAsync(memoryStream);
        }

        private static string GetAssemblyBlobName(string specificationId) => $"{specificationId}/{dllFileName}";

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
