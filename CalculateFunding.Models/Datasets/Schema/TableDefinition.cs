using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets.Schema
{
    public class TableDefinition
    {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("dataGranularity")]
        public DataGranularity DataGranularity { get; set; }

        [JsonProperty("definesProviderScope")]
        public bool DefinesTargets { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("fieldDefinitions")]
        public List<FieldDefinition> FieldDefinitions { get; set; }

        [JsonProperty("identifierFieldType")]
        public string IdentifierFieldType { get; set; }

        [JsonProperty("identifierFieldName")]
		public string IdentifierFieldName { get; set; }
    }

}