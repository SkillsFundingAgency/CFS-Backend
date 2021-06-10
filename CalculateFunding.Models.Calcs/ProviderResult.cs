using System;
using System.Collections.Generic;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.ProviderLegacy;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
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

        [JsonProperty("fundingLineResults")]
        public List<FundingLineResult> FundingLineResults { get; set; }

        [JsonProperty("isIndicativeProvider")]
        public bool IsIndicativeProvider { get; set; }
    }
}