using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Datasets
{
    public class TableDefinitionChanges
    {
        public TableDefinitionChanges()
        {
            ChangeTypes = new List<TableDefinitionChangeType>();
            FieldChanges = new List<FieldDefinitionChanges>();
        }

        [JsonProperty("tableDefinition")]
        public TableDefinition TableDefinition { get; set; }

        [JsonProperty("changeTypes")]
        public List<TableDefinitionChangeType> ChangeTypes { get; }

        [JsonProperty("fieldTypes")]
        public List<FieldDefinitionChanges> FieldChanges { get; set; }

        [JsonIgnore]
        public bool HasChanges
        {
            get
            {
                return ChangeTypes.Any() || FieldChanges.Any(m => m.HasChanges);
            }
        }
    }
}
