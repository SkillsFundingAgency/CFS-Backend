using System.IO;
using Microsoft.Azure.Cosmos;

namespace CalculateFunding.IntegrationTests.Common.Data
{
    public class ImportStream
    {
        public MemoryStream Stream { get; private set; }

        public PartitionKey PartitionKey { get; private set; }

        public string Id { get; private set; }

        public static ImportStream ForBlob(MemoryStream stream,
            string blobName)
            => new ImportStream
            {
                Stream = stream,
                Id = blobName
            };

        public static ImportStream ForCosmos(MemoryStream stream,
            string id,
            string partitionKey)
            => new ImportStream
            {
                Stream = stream,
                Id = id,
                PartitionKey = partitionKey != null ? new PartitionKey(partitionKey) : PartitionKey.None
            };
    }
}