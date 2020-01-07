using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Datasets
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AggregatedTypes
    {
        Sum,
        Average,
        Min,
        Max
    }
}
