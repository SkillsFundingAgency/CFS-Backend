using System;
using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Obsoleted
{
    [Obsolete]
    public class FundingStream : Reference
    {
        public FundingStream()
        {
            AllocationLines = new List<AllocationLine>();
            PeriodType = new PeriodType();
        }

        [JsonProperty("shortName")]
        public string ShortName { get; set; }

        [JsonProperty("allocationLines")]
        public List<AllocationLine> AllocationLines { get; set; }

        [JsonProperty("periodType")]
        public PeriodType PeriodType { get; set; }

        [JsonProperty("requireFinancialEnvelopes")]
        public bool RequireFinancialEnvelopes { get; set; }
    }
}