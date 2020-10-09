using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Services.Profiling.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RoundingStrategy
    {
        RoundUp,
        RoundDown
    }
}
