using CalculateFunding.Common.Models;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetVersion : VersionedItem
    {
        [JsonProperty("id")]
        public override string Id => $"{DatasetId}_version_{Version}";

        [JsonProperty("entityId")]
        public override string EntityId => $"{DatasetId}";

        [JsonProperty("datasetId")]
        public string DatasetId { get; set; }

        [JsonProperty("blobName")]
        public string BlobName { get; set; }

        [JsonProperty("rowCount")]
        public int RowCount { get; set; }

        [JsonProperty("newRowCount")]
        public int NewRowCount { get; set; }

        [JsonProperty("amendedRowCount")]
        public int AmendedRowCount { get; set; }

        [JsonProperty("uploadedBlobFilePath")]
        public string UploadedBlobFilePath { get; set; }

        [JsonProperty("changeType")]
        public DatasetChangeType ChangeType { get; set; }

        [JsonProperty("fundingStream")]
        public Reference FundingStream { get; set; }

        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<DatasetVersion>(json);
        }
    }
}