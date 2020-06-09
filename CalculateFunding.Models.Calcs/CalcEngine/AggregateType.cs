using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace CalculateFunding.Models.Aggregations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AggregatedType
    {
        Sum,
        Average,
        Min,
        Max,
        
    }
}
