using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class PublishedFundingStreamDefinition : Reference
    {
        public PublishedFundingStreamDefinition()
        {
            AllocationLines = new List<PublishedAllocationLineDefinition>(0);
            PeriodType = new PublishedPeriodType();
        }

        [JsonProperty("shortName")]
        public string ShortName { get; set; }

        [JsonProperty("allocationLines")]
        public List<PublishedAllocationLineDefinition> AllocationLines { get; set; }

        [JsonProperty("periodType")]
        public PublishedPeriodType PeriodType { get; set; }

        [JsonProperty("requireFinancialEnvelopes")]
        public bool RequireFinancialEnvelopes { get; set; }
    }
}
