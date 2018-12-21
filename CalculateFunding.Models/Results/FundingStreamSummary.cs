using System;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class FundingStreamSummary : Reference
    {
        [JsonProperty("shortName")]
        public string ShortName { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("publishDate")]
        public DateTimeOffset PublishDate { get; set; }

        [JsonProperty("periodType")]
        public PeriodType PeriodType { get; set; }
    }
}
