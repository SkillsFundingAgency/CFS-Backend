using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class SpecificationScopeCommand : Command<SpecificationScope>
    {
        
    }

    public class SpecificationScope : Reference
    {
        [JsonProperty("specification")]
        public Reference Specification { get; set; }
        [JsonProperty("providers")]
        public List<ProviderSummary> Providers { get; set; }

    }

    public class ProviderSummary : Reference
    {
        [JsonProperty("urn")]
        public string URN { get; set; }

        [JsonProperty("authority")]
        public Reference Authority { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("phase")]
        public Reference Phase { get; set; }        
    }

    public class ProviderResult : Reference
    {
        [JsonProperty("budget")]
        public Reference Specification { get; set; }
        [JsonProperty("provider")]
        public ProviderSummary Provider { get; set; }

        [JsonProperty("calcResults")]
        public List<CalculationResult> CalculationResults { get; set; }

        [JsonProperty("sourceDatasets")]
        public List<object> SourceDatasets { get; set; }
    }
}