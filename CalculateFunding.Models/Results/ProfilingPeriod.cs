using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
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

        [JsonIgnore]
        public DateTime PeriodDate
        {
            get => DateTime.Parse($"1 {Period} {Year}");
        }
    }
}
