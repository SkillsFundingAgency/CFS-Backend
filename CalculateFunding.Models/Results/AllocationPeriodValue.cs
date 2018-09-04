using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Results
{
    public class AllocationPeriodValue
    {
        [JsonProperty("distributionPeriod")]
        public string DistributionPeriod { get; set; }

        [JsonProperty("allocationValue")]
        public decimal AllocationValue { get; set; }
    }
}
