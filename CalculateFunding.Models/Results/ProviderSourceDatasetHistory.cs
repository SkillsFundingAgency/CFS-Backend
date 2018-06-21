using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class ProviderSourceDatasetHistory : VersionContainer<SourceDataset>
    {
        [JsonProperty("id")]
        public new string Id
        {
            get
            {
                return $"{SpecificationId}_{DataRelationship.Id}_{Provider.Id}_History";
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
    }
}