using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Datasets.Schema
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FieldType
    {
        Boolean,
        Char,
        Byte,
        Integer,
        Float,
        Decimal,
        DateTime,
        String,
        NullableOfInteger,
        NullableOfDecimal
    }
}