using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PublishedCalculationType
    {
        [EnumMember(Value = "Number")]
        Number = 0,

        [EnumMember(Value = "Funding")]
        Funding = 10,

        [EnumMember(Value = "Baseline")]
        Baseline = 20,
    }
}
