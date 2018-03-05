using System.Collections.Generic;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class BuildProject : Reference
    {
        [JsonProperty("specification")]
        public SpecificationSummary Specification { get; set; }

        [JsonProperty("targetLanguage")]
        public TargetLanguage TargetLanguage { get; set; } = TargetLanguage.VisualBasic;

        [JsonProperty("calculations")]
        public List<Calculation> Calculations { get; set; }

        [JsonProperty("datasetDefinitions")]
        public List<DatasetDefinition> DatasetDefinitions { get; set; }

        [JsonProperty("build")]
        public Build Build { get; set; }

    }
}