using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Months
    {
        January = 1,
        February,
        March,
        April,
        May,
        June,
        July,
        August,
        September,
        October,
        November,
        December
    }
}
