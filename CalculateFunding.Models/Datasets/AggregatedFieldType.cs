using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Datasets
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AggregatedFieldType
    {
        Sum,
        Average,
        Min,
        Max
    }
}
