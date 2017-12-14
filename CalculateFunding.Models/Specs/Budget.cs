using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class Budget : Reference
    {

        [JsonProperty("acronym")]
        public string Acronym { get; set; }

        [JsonProperty("academicYear")]
        public string AcademicYear { get; set; }

        [JsonProperty("fundingStream")]
        public string FundingStream { get; set; }

        [JsonProperty("fundingPolicies")]
        public List<FundingPolicy> FundingPolicies { get; set; }

        [JsonProperty("datasetDefinitions")]
        public List<DatasetDefinition> DatasetDefinitions { get; set; }

        [JsonProperty("targetLanguage")]
        public TargetLanguage TargetLanguage { get; set; }

    }
}

