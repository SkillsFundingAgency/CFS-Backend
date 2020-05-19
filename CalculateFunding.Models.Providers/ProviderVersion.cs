using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Providers
{
    public class ProviderVersion: ProviderVersionMetadata, IIdentifiable
    {
        [JsonProperty("id")]
        public new string Id { get; set; }

        [JsonProperty("providers")]
        public IEnumerable<Provider> Providers { get; set; }
    }
}
