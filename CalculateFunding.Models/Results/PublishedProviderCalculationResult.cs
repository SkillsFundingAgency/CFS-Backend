using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderCalculationResult : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("specification")]
        public Reference Specification { get; set; }

        [JsonProperty("calculationSpecification")]
        public Reference CalculationSpecification { get; set; }

        [JsonProperty("policy")]
        public Reference Policy { get; set; }

        [JsonProperty("parentPolicy")]
        public Reference ParentPolicy { get; set; }

        [JsonProperty("current")]
        public PublishedProviderCalculationResultCalculationVersion Current { get; set; }

        [JsonProperty("published")]
        public PublishedProviderCalculationResultCalculationVersion Published { get; set; }

        [JsonProperty("approved")]
        public PublishedProviderCalculationResultCalculationVersion Approved { get; set; }
    }

}
