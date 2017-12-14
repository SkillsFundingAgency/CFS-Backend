using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Scenarios
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TestStepType
    {
        GivenSourceField,
        ThenProductValue,
        ThenSourceField,
        ThenExceptionNotThrown
    }
}