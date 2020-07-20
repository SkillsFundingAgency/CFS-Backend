using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Providers
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProviderSource
    {
        CFS = 0,
        FDZ = 1
    }
}
