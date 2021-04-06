using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Graph
{
    [Serializable]
    public class FundingLine : SpecificationNode
    {
        public const string IdField = "specificationfundinglineid";

        [JsonProperty(IdField)]
        public string SpecificationFundingLineId => $"{SpecificationId}-{FundingLineId}";

        [JsonProperty("fundinglineid")]
        public string FundingLineId { get; set; }

        [JsonProperty("fundinglinename")]
        public string FundingLineName { get; set; }
    }
}
