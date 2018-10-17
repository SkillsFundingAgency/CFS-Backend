using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationSummaryModel : Reference
    {
        [JsonProperty("calculationType")]
        public CalculationType CalculationType { get; set; }

        [JsonProperty("isPublic")]
        public bool IsPublic { get; set; }

        [JsonProperty("status")]
        public PublishStatus Status { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }
    }
}
