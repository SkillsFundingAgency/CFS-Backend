using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace CalculateFunding.Models.Datasets
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TableDefinitionChangeType
    {
        [EnumMember(Value = "DefinitionName")]
        DefinitionName
    }
}
