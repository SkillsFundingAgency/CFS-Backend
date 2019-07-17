using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Publishing
{
    public class ProviderResult : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("provider")]
        public ProviderSummary Provider { get; set; }

        [JsonProperty("calcResults")]
        public List<CalculationResult> CalculationResults { get; set; }

        [JsonProperty("allocationLineResults")]
        public List<AllocationLineResult> AllocationLineResults { get; set; }

        [JsonProperty("sourceDatasets")]
        public List<object> SourceDatasets { get; set; }
    }
}
