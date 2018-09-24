using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class ProviderSourceDatasetHistory : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{SpecificationId}_{DataRelationship.Id}_{ProviderId}_History";
            }
        }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

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

        [JsonProperty("history")]
        public IEnumerable<ProviderSourceDatasetVersion> History { get; set; } = Enumerable.Empty<ProviderSourceDatasetVersion>();

        public int GetNextVersion()
        {
            if (History == null || !History.Any())
                return 1;

            int maxVersion = History.Max(m => m.Version);

            return maxVersion + 1;
        }
    }
}