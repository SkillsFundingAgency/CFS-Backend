using System;
using System.Collections.Generic;
using CalculateFunding.Models.Obsoleted;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class PublishedFundingStreamAllocationLine
    {
        [JsonProperty("allocationLine")]
        public AllocationLine AllocationLine { get; set; }

        [JsonProperty("allocationLineVersionNumber")]
        public int AllocationLineVersionNumber { get; set; }

        [JsonProperty("allocationLineStatus")]
        public AllocationLineStatus AllocationLineStatus { get; set; }

        [JsonProperty("AllocationAmount")]
        public decimal AllocationAmount { get; set; }

        [JsonProperty("profilePeriods")]
        public IEnumerable<ProfilingPeriod> ProfilePeriods { get; }

        [JsonProperty("financialEnvelopes")]
        public IEnumerable<FinancialEnvelope> FinancialEnvelopes { get; }

        [JsonProperty("allocationMajorVersion")]
        public int AllocationMajorVersion { get; set; }

        [JsonProperty("allocationMinorVerion ")]
        public int AllocationMinorVerion { get; set; }

        [JsonProperty("calculations")]
        public IEnumerable<FundingStreamCalculation> Calculations { get; set; }
    }
}
