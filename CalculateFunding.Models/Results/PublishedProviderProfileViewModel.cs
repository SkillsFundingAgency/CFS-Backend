using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
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
