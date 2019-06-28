using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Policy
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FundingRoute
    {
        Provider,
        LA
    }
}
