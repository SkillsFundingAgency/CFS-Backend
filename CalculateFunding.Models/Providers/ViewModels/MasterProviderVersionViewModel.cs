using Newtonsoft.Json;

namespace CalculateFunding.Models.Providers.ViewModels
{
    public class MasterProviderVersionViewModel
    {
        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }
    }
}
