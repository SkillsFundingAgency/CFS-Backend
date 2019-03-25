using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Search
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SearchMode
    {
        Any = 0,
        All = 1
    }
}
