using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Publishing
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AggregationType
    {
        None,
        
        Average,
        
        Sum,
        
        GroupRate,
        
        PercentageChangeBetweenAandB
    }
}
