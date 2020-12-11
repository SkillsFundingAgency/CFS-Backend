using System.Collections.Generic;
using System.Linq;
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

        public bool HasSameIdentifier(RowLoadResult other)
            => Identifier == other?.Identifier &&
               IdentifierFieldType == other?.IdentifierFieldType;

        public bool HasDifferentFieldValues(RowLoadResult other)
            => Fields.Any(x => 
                other.Fields.Any(y => y.Key == x.Key && 
                                      y.Value?.Equals(x.Value) == false));
    }
}