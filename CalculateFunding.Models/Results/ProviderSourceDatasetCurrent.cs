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
                return $"{SpecificationId}_{DataRelationship?.Id}_{ProviderId}";
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

        // Not included to reduce updates to Current - if the row values are not changed, then no need to write to cosmos. Can look this value up from relationship if required.
        //[JsonProperty("dataset")]
        //public VersionReference Dataset { get; set; }

        [JsonProperty("rows")]
        public List<Dictionary<string, object>> Rows { get; set; }
    }
}
