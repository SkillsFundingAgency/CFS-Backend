using System;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationResponseModel : Reference
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("sourceCode")]
        public string SourceCode { get; set; }

        [JsonProperty("calculationType")]
        public CalculationType CalculationType { get; set; }

        [JsonProperty("sourceCodeName")]
        public string SourceCodeName { get; set; }

        [JsonProperty("namespace")]
        public CalculationNamespace Namespace { get; set; }

        [JsonProperty("wasTemplateCalculation")]
        public bool WasTemplateCalculation { get; set; }

        [JsonProperty("valueType")]
        public CalculationValueType ValueType { get; set; }

        [JsonProperty("lastUpdated")]
        public DateTimeOffset? LastUpdated { get; set; }

        [JsonProperty("author")]
        public Reference Author { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("publishStatus")]
        public PublishStatus PublishStatus { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
