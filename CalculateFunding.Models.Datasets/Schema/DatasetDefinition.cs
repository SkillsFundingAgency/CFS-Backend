using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets.Schema
{
    public class DatasetDefinition : Reference
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("version")]
        public int? Version { get; set; }

        [JsonProperty("tableDefinitions")]
        public List<TableDefinition> TableDefinitions { get; set; }

        [JsonProperty("converterEligible")]
        public bool ConverterEligible { get; set; }

        [JsonProperty("validateProviders")]
        public bool ValidateProviders { get; set; }

        [JsonProperty("validateProvidersByYearRange")]
        public int? ValidateProvidersByYearRange { get; set; }
    }

}