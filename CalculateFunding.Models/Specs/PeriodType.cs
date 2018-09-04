using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Specs
{
    public class PeriodType : Reference
    {
        [JsonProperty("startDay")]
        public int StartDay { get; set; }

        [JsonProperty("startMonth")]
        public int StartMonth { get; set; }

        [JsonProperty("endDay")]
        public int EndDay { get; set; }

        [JsonProperty("endMonth")]
        public int EndMonth { get; set; }
    }
}
