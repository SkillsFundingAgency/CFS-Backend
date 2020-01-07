using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Providers
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProviderVersionType
    {
        Missing,
        Custom,
        SystemImported,
    }
}
