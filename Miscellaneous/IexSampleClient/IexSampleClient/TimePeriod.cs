using Newtonsoft.Json;
using System;

namespace IexSampleClient
{
    public class TimePeriod
    {
        [JsonProperty("periodType")]
        public string PeriodType { get; set; }

        [JsonProperty("periodId")]
        public string PeriodId { get; set; }

        [JsonProperty("startDate")]
        public DateTimeOffset StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTimeOffset EndDate { get; set; }
    }
}
