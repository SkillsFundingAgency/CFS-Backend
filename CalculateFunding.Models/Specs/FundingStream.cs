using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class FundingStream : Reference
    {
        public FundingStream()
        {
            AllocationLines = new List<AllocationLine>();
        }

        [JsonProperty("shortName")]
        public string ShortName { get; set; }

        [JsonProperty("allocationLines")]
        public List<AllocationLine> AllocationLines { get; set; }

        [JsonProperty("periodType")]
        public PeriodType PeriodType { get; set; }
    }
}