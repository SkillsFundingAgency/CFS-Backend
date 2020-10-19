using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Services.Profiling.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProfileConfigurationType
    {
        RuleBased,
        Custom,
    }
}
