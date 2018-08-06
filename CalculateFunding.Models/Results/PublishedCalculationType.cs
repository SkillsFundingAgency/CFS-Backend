using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Results
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PublishedCalculationType
    {
        [EnumMember(Value = "Number")]
        Number = 0,

        [EnumMember(Value = "Funding")]
        Funding = 10
    }
}
