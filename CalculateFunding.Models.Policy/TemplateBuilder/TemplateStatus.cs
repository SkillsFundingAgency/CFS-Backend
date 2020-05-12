using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Policy.TemplateBuilder
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TemplateStatus
    {
        Draft,
        Published
    }
}