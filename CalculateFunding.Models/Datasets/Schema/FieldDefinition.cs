using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets.Schema
{
    public class FieldDefinition
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("longName")]
        public string LongName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("type")]
        public FieldType Type { get; set; }

    }
}