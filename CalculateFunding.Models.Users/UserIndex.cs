using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Users
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "userindex")]
    public class UserIndex
    {
        [Key]
        [IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("name")]
        public string Name { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("userName")]
        public string Username { get; set; }
    }
}
