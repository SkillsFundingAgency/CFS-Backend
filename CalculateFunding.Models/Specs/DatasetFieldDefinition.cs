using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class DatasetFieldDefinition
    {
        [JsonProperty("id")]
        public string Id => Name.ToSlug();

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