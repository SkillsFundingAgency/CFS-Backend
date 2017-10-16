using Newtonsoft.Json;

namespace Allocations.Models.Framework
{
    public class ProviderDataset : IProviderDataset
    {
        [JsonProperty("id")]
        public string Id => $"{ModelName}-{DatasetName}-{URN}";
        public string ModelName { get; set; }
        public string DatasetName { get; set; }
        public string URN { get; set; }
    }
}