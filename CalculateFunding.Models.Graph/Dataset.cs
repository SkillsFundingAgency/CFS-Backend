using Newtonsoft.Json;

namespace CalculateFunding.Models.Graph
{
    
    public class Dataset
    {
        public const string IdField = "datasetid";

        [JsonProperty(IdField)]
        public string DatasetId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }    
        
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}