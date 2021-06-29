using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Datasets
{
    public class PublishedSpecificationTemplateMetadata
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("templateId")]
        public uint TemplateId { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PublishedSpecificationTemplateMetadataType Type { get; set; }
    }
}
