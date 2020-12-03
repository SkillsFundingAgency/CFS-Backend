namespace CalculateFunding.Services.Publishing.Batches
{
    public class BatchUploadBlobName
    {
        public BatchUploadBlobName(string batchId)
        {
            BatchId = batchId;
        }
        public string BatchId { get; }

        public static implicit operator string(BatchUploadBlobName blobName)
            => blobName.ToString();

        public override string ToString() => $"{BatchId}/request.xlsx";
    }
}