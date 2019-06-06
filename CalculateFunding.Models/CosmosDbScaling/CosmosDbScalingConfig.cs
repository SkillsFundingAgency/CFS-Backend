using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.CosmosDbScaling
{
    public class CosmosDbScalingConfig : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("repositoryType")]
        public CosmosRepositoryType RepositoryType { get; set; }

        [JsonProperty("baseRequestUnits")]
        public int BaseRequestUnits { get; set; }

        [JsonProperty("maxRequestUnits")]
        public int MaxRequestUnits { get; set; }

        [JsonProperty("currentRequestUnits")]
        public int CurrentRequestUnits { get; set; }

        [JsonProperty("jobRequestUnitConfigs")]
        public IEnumerable<CosmosDbScalingJobConfig> JobRequestUnitConfigs { get; set; }

        [JsonIgnore]
        public int AvailableRequestUnits => MaxRequestUnits - CurrentRequestUnits;

        [JsonIgnore]
        public bool IsAtBaseLine => CurrentRequestUnits == BaseRequestUnits;
    }
}
