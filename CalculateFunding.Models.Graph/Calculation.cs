using Newtonsoft.Json;
using System;

namespace CalculateFunding.Models.Graph
{
    [Serializable]
    public class Calculation
    {
        public const string IdField = "calculationid";

        [JsonProperty("calculationid")]
        public string CalculationId { get; set; }

        [JsonProperty("specificationid")]
        public string SpecificationId { get; set; }

        [JsonProperty("calculationname")]
        public string CalculationName { get; set; }

        [JsonProperty("calculationtype")]
        public CalculationType CalculationType { get; set; }

        [JsonProperty("fundingstream")]
        public string FundingStream { get; set; }

        [JsonProperty("templatecalculationid", NullValueHandling = NullValueHandling.Ignore)]
        public string TemplateCalculationId { get; set; }
    }
}
