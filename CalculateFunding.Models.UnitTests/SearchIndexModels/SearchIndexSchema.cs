using Newtonsoft.Json;

namespace CalculateFunding.Models.UnitTests.SearchIndexModels
{
    public class SearchIndexSchema
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("fields")]
        public SearchIndexField[] Fields { get; set; }
    }
}
