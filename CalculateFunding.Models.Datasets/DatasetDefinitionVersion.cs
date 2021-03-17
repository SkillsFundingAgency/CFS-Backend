using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetDefinitionVersion : Reference
    {
        [JsonProperty("version")]
        public int? Version { get; set; }
    }
}
