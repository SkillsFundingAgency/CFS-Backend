using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Providers
{
    public class MasterProviderVersion : ProviderVersionMetadata, IIdentifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
