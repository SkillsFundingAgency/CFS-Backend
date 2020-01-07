using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Scenarios
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TestResult
    {
        Inconclusive,
        Failed,
        Passed,
        Ignored
    }
}