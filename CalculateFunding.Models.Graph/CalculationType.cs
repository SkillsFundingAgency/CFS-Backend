using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Graph
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CalculationType
    {
        Additional,
        Template
    }
}
