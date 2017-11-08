using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Allocations.Models.Results
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