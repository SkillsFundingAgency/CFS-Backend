using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderCalculationResultPolicy
    {
        public PublishedProviderCalculationResultPolicy()
        {
            SubPolicies = Enumerable.Empty<PublishedProviderCalculationResultPolicy>();

            CalculationResults = Enumerable.Empty<PublishedCalculationResult>();
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        public IEnumerable<PublishedProviderCalculationResultPolicy> SubPolicies { get; set;  }

        public IEnumerable<PublishedCalculationResult> CalculationResults { get; set; }
    }

}
