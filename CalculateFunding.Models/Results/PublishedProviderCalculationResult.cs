using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderCalculationResult : IIdentifiable
    {
        public PublishedProviderCalculationResult()
        {
            Policies = Enumerable.Empty<PublishedProviderCalculationResultPolicy>();
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("ukprn")]
        public string Ukprn { get; set; }

        [JsonProperty("specification")]
        public Reference Specification { get; set; }

        [JsonProperty("policies")]
        public IEnumerable<PublishedProviderCalculationResultPolicy> Policies { get; set; }
    }

}
