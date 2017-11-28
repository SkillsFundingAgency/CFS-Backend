using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class ProviderResult : DocumentEntity
    {
        public override string Id => $"{DocumentType}-{Budget.Id}-{Provider.Id}".ToSlug();

        [JsonProperty("budget")]
        public Reference Budget { get; set; }
        [JsonProperty("provider")]
        public Reference Provider { get; set; }

        [JsonProperty("sourceDatasets")]
        public object[] SourceDatasets { get; set; }

        [JsonProperty("products")]
        public ProductResult[] ProductResults { get; set; }
    }
}