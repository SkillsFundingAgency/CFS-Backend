using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationFundingLine
    {
        [JsonProperty("templateId")]
        public uint TemplateId { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}