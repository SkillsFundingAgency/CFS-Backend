using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Policy.TemplateBuilder
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TemplateStatus
    {
        Draft = 0,
        Approved = 1,
        Updated = 2
    }
}