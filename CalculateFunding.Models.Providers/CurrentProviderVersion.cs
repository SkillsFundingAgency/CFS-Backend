using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Providers
{
    public class CurrentProviderVersion : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }
    }
}