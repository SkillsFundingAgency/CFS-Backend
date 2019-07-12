using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.CosmosDbScaling
{
    public class CosmosDbScalingConfig : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("repositoryType")]
        public CosmosCollectionType RepositoryType { get; set; }

        [JsonProperty("jobRequestUnitConfigs")]
        public IEnumerable<CosmosDbScalingJobConfig> JobRequestUnitConfigs { get; set; }
    }
}
