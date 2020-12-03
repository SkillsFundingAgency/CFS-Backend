namespace CalculateFunding.Services.Publishing.Batches
{
    public class BatchUploadProviderIdsBlobName
    {
        public BatchUploadProviderIdsBlobName(string batchId)
        {
            BatchId = batchId;
        }
        public string BatchId { get; }

        public static implicit operator string(BatchUploadProviderIdsBlobName blobName)
            => blobName.ToString();

        public override string ToString() => $"{BatchId}/publishedProviders.json";
    }
}