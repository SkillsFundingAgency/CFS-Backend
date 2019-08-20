using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class AllocationPeriodValue
    {
        [JsonProperty("distributionPeriod")]
        public string DistributionPeriod { get; set; }

        [JsonProperty("allocationValue")]
        public decimal AllocationValue { get; set; }
    }
}
