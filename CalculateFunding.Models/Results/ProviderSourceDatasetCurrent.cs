using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class ProviderSourceDatasetCurrent : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{SpecificationId}_{DataRelationship?.Id}_{Provider?.Id}";
            }
        }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("provider")]
        public Reference Provider { get; set; }

        [JsonProperty("dataDefinition")]
        public Reference DataDefinition { get; set; }

        [JsonProperty("dataRelationship")]
        public Reference DataRelationship { get; set; }

        [JsonProperty("dataGranularity")]
        public DataGranularity DataGranularity { get; set; }

        [JsonProperty("definesScope")]
        public bool DefinesScope { get; set; }

        [JsonProperty("dataset")]
        public VersionReference Dataset { get; set; }

        [JsonProperty("rows")]
        public List<Dictionary<string, object>> Rows { get; set; }
    }
}
