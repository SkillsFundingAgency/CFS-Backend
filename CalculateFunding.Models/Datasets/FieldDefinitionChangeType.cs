using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace CalculateFunding.Models.Datasets
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FieldDefinitionChangeType
    {
        [EnumMember(Value = "IsAggregable")]
        IsAggregable,
        [EnumMember(Value = "IsNotAggregable")]
        IsNotAggregable,
        [EnumMember(Value = "FieldName")]
        FieldName,
        [EnumMember(Value = "AddedField")]
        AddedField,
        [EnumMember(Value = "RemoveField")]
        RemovedField,
        [EnumMember(Value = "FieldType")]
        FieldType,
        [EnumMember(Value = "IdentifierType")]
        IdentifierType
    }
}
