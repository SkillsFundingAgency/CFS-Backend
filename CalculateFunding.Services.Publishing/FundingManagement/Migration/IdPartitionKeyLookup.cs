using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Publishing.FundingManagement.Migration
{
    public class IdPartitionKeyLookup : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("partitionKey")]
        public string ParitionKey { get; set; }
    }
}
