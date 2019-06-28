using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.FundingPolicy
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrganisationGroupingType
    {
        LACode,
        UKPRN,
        TrustCode
    }
}