using Newtonsoft.Json;

namespace CalculateFunding.Services.Publishing.Batches
{
    public class BatchUploadValidationRequest
    {
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }
        
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }
        
        [JsonProperty("batchId")]
        public string BatchId { get; set; }
    }
}