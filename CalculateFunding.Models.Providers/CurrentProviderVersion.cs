using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Providers
{
    public class CurrentProviderVersion : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }

        [JsonProperty("providerSnapshotId")]
        public int? ProviderSnapshotId { get; set; }

        [JsonProperty("fundingPeriod")]
        public List<ProviderSnapShotByFundingPeriod> FundingPeriod { get; set; }
    }
}