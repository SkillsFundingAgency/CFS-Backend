using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetDefinitionChanges
    {
        public DatasetDefinitionChanges()
        {
            DefinitionChanges = new List<DefinitionChangeType>();
            TableDefinitionChanges = new List<TableDefinitionChanges>();
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tableDefinitionChanges")]
        public List<TableDefinitionChanges> TableDefinitionChanges { get; }

        [JsonProperty("definitionChanges")]
        public List<DefinitionChangeType> DefinitionChanges { get;  }

        [JsonProperty("newName")]
        public string NewName { get; set; }

        [JsonIgnore]
        public bool HasChanges
        {
            get { return TableDefinitionChanges.Any(m => m.HasChanges) || DefinitionChanges.Any(); }
        }
    }
}
