using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Results
{
    public class PublishedAllocationLineResultHistory : IIdentifiable
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("allocationResultId")]
        public string AllocationResultId { get; set; }

        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }

        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{AllocationResultId}_hist";
            }
        }

        [JsonProperty("history")]
        public IEnumerable<PublishedAllocationLineResultVersion> History { get; set; }
    }
}
