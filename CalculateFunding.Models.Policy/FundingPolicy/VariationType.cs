using Newtonsoft.Json;

namespace CalculateFunding.Models.FundingPolicy
{
    public class VariationType
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("order")]
        public int Order { get; set; }
    }
}