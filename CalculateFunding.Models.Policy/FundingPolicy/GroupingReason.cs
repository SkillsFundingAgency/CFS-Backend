using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Policy.FundingPolicy
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GroupingReason
    {
        Payment,
        Information,
        Contracting,
        Indicative
    }
}