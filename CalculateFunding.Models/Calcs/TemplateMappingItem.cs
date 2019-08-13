using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class TemplateMappingItem
    {
        [JsonProperty("entityType")]
        public TemplateMappingEntityType EntityType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("templateId")]
        public uint TemplateId { get; set; }

        [JsonProperty("calculationId")]
        public string CalculationId { get; set; }
    }
}
