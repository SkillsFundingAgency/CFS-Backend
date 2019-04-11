using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Datasets
{
    public class FieldDefinitionChanges
    {
        public FieldDefinitionChanges()
        {
            ChangeTypes = new List<FieldDefinitionChangeType>();
        }

        public FieldDefinitionChanges(FieldDefinitionChangeType fieldDefinitionChangeType)
        {
            ChangeTypes = new List<FieldDefinitionChangeType> { fieldDefinitionChangeType };
        }

        [JsonProperty("fieldDefinition")]
        public FieldDefinition FieldDefinition { get; set; }

        [JsonProperty("changeTypes")]
        public List<FieldDefinitionChangeType> ChangeTypes { get;  }

        [JsonIgnore]
        public bool HasChanges
        {
            get
            {
                return ChangeTypes.Any();
            }
        }
    }
}
