using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Allocations.Models.Specs
{
    public class Budget : DocumentEntity
    {
        public override string Id => $"{DocumentType}-{Acronym}".ToSlug();

        [JsonProperty("name")]
        public string Name { get; set; }

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

