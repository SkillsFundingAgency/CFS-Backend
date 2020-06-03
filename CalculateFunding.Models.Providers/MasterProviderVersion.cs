using Newtonsoft.Json;

namespace CalculateFunding.Models.Providers
{
    public class MasterProviderVersion : ProviderVersionMetadata
    {
        [JsonProperty("id")]
        public new string Id { get; set; }
    }
}
