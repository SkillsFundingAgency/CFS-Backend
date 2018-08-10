using CalculateFunding.Models.Specs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderResultsPolicySummary
    {
        public PublishedProviderResultsPolicySummary()
        {
            Policies = new PublishedProviderResultsPolicySummary[0];
            Calculations = new PublishedProviderResultsCalculationSummary[0];
        }
    
        [JsonProperty("policy")]
        public PolicySummary Policy { get; set; }

        [JsonProperty("policies")]
        public PublishedProviderResultsPolicySummary[] Policies { get; set; }

        [JsonProperty("calculations")]
        public PublishedProviderResultsCalculationSummary[] Calculations { get; set; }
    }
}
