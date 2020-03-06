using Newtonsoft.Json;

namespace CalculateFunding.Models.Graph
{
    public class DataField
    {
        public const string IdField = "datafieldid";

        [JsonProperty(IdField)]
        public string DataFieldId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }  
        
        [JsonProperty("fieldName")]
        public string FieldName { get; set; }
    }
}