using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Results
{
    public class ProfilingPeriod
    {
        [JsonProperty("period")]
        public string Period { get; set; }

        [JsonProperty("occurrence")]
        public int Occurrence { get; set; }

        [JsonProperty("periodYear")]
        public int Year { get; set; }

        [JsonProperty("periodType")]
        public string Type { get; set; }

        [JsonProperty("profileValue")]
        public decimal Value { get; set; }

        [JsonProperty("distributionPeriod")]
        public string DistributionPeriod { get; set; }
    }
}
