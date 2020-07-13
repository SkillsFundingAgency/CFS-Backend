using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Models
{
    public class ProviderInformation
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}