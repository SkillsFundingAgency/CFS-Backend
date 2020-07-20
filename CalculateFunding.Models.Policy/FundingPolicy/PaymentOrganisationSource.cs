using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Policy.FundingPolicy
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PaymentOrganisationSource
    {
        Undefined = 0,
        PaymentOrganisationAsProvider = 1,
        PaymentOrganisationFields = 2
    }
}
