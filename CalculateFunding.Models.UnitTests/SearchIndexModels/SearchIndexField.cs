using Newtonsoft.Json;

namespace CalculateFunding.Models.UnitTests.SearchIndexModels
{
    public class SearchIndexField
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("key")]
        public bool Key { get; set; }

        [JsonProperty("facetable")]
        public bool Facetable { get; set; }

        [JsonProperty("filterable")]
        public bool Filterable { get; set; }

        [JsonProperty("retrievable")]
        public bool Retrievable { get; set; }

        [JsonProperty("searchable")]
        public bool Searchable { get; set; }

        [JsonProperty("sortable")]
        public bool Sortable { get; set; }
    }
}
