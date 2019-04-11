using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderProfileViewModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("profilePeriods")]
        public IEnumerable<ProfilingPeriod> ProfilingPeriods { get; set; }

        [JsonProperty("financialEnvelopes")]
        public IEnumerable<FinancialEnvelope> FinancialEnvelopes { get; set; }
    }
}
