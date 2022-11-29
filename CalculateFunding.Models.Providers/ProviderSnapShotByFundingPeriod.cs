using Newtonsoft.Json;

namespace CalculateFunding.Models.Providers
{
    public class ProviderSnapShotByFundingPeriod
    {
        [JsonProperty("fundingPeriodName")]
        public string FundingPeriodName { get; set; }

        [JsonProperty("providerSnapshotId")]
        public int? ProviderSnapshotId { get; set; }

        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }
    }
}
