using System;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Policy
{
    public class FundingPeriod : Reference
    {
        [JsonProperty("startDate")]
        public DateTimeOffset StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTimeOffset EndDate { get; set; }

        [JsonProperty("period")]
        public string Period { get; set; }

        [JsonProperty("type")]
        public FundingPeriodType Type { get; set; }

        [JsonProperty("startYear")]
        public int StartYear
        {
            get
            {
                return StartDate.Year;
            }
        }

        [JsonProperty("endYear")]
        public int EndYear
        {
            get
            {
                return EndDate.Year;
            }
        }
    }

}
