using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Graph
{
    [Serializable]
    public class FundingLine
    {
        public const string IdField = "fundinglineid";

        [JsonProperty("fundinglineid")]
        public string FundingLineId { get; set; }

        [JsonProperty("fundinglinename")]
        public string FundingLineName { get; set; }
    }
}
