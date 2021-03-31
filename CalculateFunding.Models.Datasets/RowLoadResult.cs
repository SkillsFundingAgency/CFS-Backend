using System;
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
        {
            return Identifier == other?.Identifier && IdentifierFieldType == other?.IdentifierFieldType;
        }

        public bool HasDifferentFieldValues(RowLoadResult other)
        {
            return Fields.Any(x => other.Fields.Any(y => y.Key == x.Key && y.Value?.Equals(x.Value) == false));
        }

        public RowLoadResult CopyRow(string identifier, IdentifierFieldType identifierFieldType,
            params (string fieldName, object value)[] fieldOverrides)
        {
            RowLoadResult copy = new RowLoadResult
            {
                Identifier = identifier,
                IdentifierFieldType = identifierFieldType,
                Fields = Fields?.ToDictionary(_ => _.Key, _ => _.Value) ?? new Dictionary<string, object>()
            };

            foreach ((string fieldName, object value) fieldOverride in fieldOverrides ?? ArraySegment<(string fieldName, object value)>.Empty)
            {
                copy.Fields[fieldOverride.fieldName] = fieldOverride.value;
            }

            return copy;
        }
    }
}