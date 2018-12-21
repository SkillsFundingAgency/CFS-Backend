using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets.Schema
{
    public class DatasetDefinition : Reference
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("tableDefinitions")]
        public List<TableDefinition> TableDefinitions { get; set; }
    }

}