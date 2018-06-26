using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderResult : IIdentifiable
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("ukprn")]
        public string Ukprn { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("fundingStreamResults")]
        public IEnumerable<PublishedFundingStreamResult> FundingStreamResults { get; set; }
    }
}
