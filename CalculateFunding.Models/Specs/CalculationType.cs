using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Specs
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CalculationType
    {
        Number = 0,
        Funding = 10,
        Baseline = 20,
    }
}
