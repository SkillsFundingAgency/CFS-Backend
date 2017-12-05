using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Allocations.Services.Compiler
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