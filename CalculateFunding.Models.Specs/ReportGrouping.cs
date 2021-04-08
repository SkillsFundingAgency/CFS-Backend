using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Specs
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ReportGrouping
    {
        Undefined,
        Live,
        Provider,
        Group,
        Profiling,
    }
}
