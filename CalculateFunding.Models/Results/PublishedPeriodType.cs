﻿using System;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class PublishedPeriodType : Reference
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
