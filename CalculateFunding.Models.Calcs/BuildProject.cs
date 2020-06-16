using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class BuildProject : Reference
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("targetLanguage")]
        public TargetLanguage TargetLanguage { get; set; } = TargetLanguage.VisualBasic;

        [JsonProperty("datasetRelationships")]
        public List<DatasetRelationshipSummary> DatasetRelationships { get; set; }

        [JsonProperty("fundingLines")]
        public IDictionary<string, Funding> FundingLines { get; set; }

        [JsonProperty("build")]
        public Build Build { get; set; }
    }
}