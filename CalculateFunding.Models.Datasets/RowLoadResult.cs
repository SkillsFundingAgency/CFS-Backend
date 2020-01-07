using System.Collections.Generic;
using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class RowLoadResult
    {
        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        [JsonProperty("identifierFieldType")]
        public IdentifierFieldType IdentifierFieldType { get; set; }

        [JsonProperty("fields")]
        public Dictionary<string, object> Fields { get; set; }

    }
}