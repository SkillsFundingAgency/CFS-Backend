using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Specs
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CoreProviderVersionUpdates
    {
        Manual = 0,
        UseLatest = 1
    }
}
