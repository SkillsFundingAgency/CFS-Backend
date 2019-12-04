using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class ProviderSourceDataset : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id => $"{SpecificationId}_{DataRelationship?.Id}_{ProviderId}";

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("dataDefinitionId")]
        public string DataDefinitionId { get; set; }

        [JsonProperty("dataDefinition")]
        public Reference DataDefinition { get; set; }

        [JsonProperty("dataRelationship")]
        public Reference DataRelationship { get; set; }

        [JsonProperty("datasetRelationshipSummary")]
        public Reference DatasetRelationshipSummary { get; set; }

        [JsonProperty("dataGranularity")]
        public DataGranularity DataGranularity { get; set; }

        [JsonProperty("definesScope")]
        public bool DefinesScope { get; set; }

        [JsonProperty("current")]
        public ProviderSourceDatasetVersion Current { get; set; }
    }
}
