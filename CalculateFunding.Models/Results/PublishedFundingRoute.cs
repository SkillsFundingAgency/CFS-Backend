using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Results
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PublishedFundingRoute
    {
        Provider,
        LA
    }
}
