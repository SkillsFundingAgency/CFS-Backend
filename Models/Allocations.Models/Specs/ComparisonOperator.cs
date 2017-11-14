using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Allocations.Models.Specs
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ComparisonOperator
    {
        [EnumMember(Value = "is equal to")]
        EqualTo,
        [EnumMember(Value = "is not equal to")]
        NotEqualTo,
        [EnumMember(Value = "is greater than")]
        GreaterThan,
        [EnumMember(Value = "is less than")]
        LessThan,
        [EnumMember(Value = "is greater than or equal to")]
        GreaterThanOrEqualTo,
        [EnumMember(Value = "is less than or equal to")]
        LessThanOrEqualTo
    }
}