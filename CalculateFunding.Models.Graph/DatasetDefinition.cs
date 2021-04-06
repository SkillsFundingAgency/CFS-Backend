using Newtonsoft.Json;

namespace CalculateFunding.Models.Graph
{
    public class DatasetDefinition : SpecificationNode
    {
        public const string IdField = "datasetdefinitionid";

        [JsonProperty(IdField)]
        public string DatasetDefinitionId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }   
        
        [JsonProperty("description")]
        public string Description { get; set; }    
    }
}