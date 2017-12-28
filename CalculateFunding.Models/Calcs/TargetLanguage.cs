using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Calcs
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TargetLanguage
    {
        CSharp,
        VisualBasic
    }
}