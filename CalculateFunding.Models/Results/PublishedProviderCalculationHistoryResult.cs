using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderCalculationResultHistory : IIdentifiable
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("allocationResultId")]
        public string CalculationnResultId { get; set; }

        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{CalculationnResultId}_hist";
            }
        }

        [JsonProperty("history")]
        public IEnumerable<PublishedProviderCalculationResultVersion> History { get; set; }
    }
}
