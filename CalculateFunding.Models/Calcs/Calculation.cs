using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class Calculation : Reference
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("current")]
        public CalculationVersion Current { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("name")]
        public new string Name => Current?.Name;
        
        [JsonIgnore] public string Namespace => Current.Namespace == CalculationNamespace.Additional ? "Calculations" : FundingStreamId;
    }
}