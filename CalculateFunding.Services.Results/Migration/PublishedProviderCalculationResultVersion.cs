using CalculateFunding.Models.Results;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Migration
{
    public class PublishedProviderCalculationResultVersion : VersionedItem
    {
        [JsonProperty("id")]
        public override string Id
        {
            get { return $"{CalculationResultId}_version_{Version}"; }
        }

        [JsonProperty("entityId")]
        public override string EntityId
        {
            get { return $"{CalculationResultId}"; }
        }

        [JsonProperty("allocationResultId")]
        public string CalculationResultId { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("calculationVersion")]
        public int CalculationVersion { get; set; }

        [JsonProperty("calculationType")]
        public PublishedCalculationType CalculationType { get; set; }

        [JsonProperty("provider")]
        public ProviderSummary Provider { get; set; }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedProviderCalculationResultVersion>(json);
        }
    }
}
