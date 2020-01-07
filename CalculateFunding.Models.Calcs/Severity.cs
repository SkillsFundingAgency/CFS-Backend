using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Calcs
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Severity
    {
        Hidden,
        Info,
        Warning,
        Error,
    }
}