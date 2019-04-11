using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets.Schema
{
    public class TableDefinition
    {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("fieldDefinitions")]
        public List<FieldDefinition> FieldDefinitions { get; set; }
    }
}