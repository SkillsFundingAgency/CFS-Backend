using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Api.Providers.ViewModels
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProviderVersionType
    {
        Custom,
        SystemImported,
    }
}
