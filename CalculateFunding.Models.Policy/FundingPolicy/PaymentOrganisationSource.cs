using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Policy.FundingPolicy
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PaymentOrganisationSource    {
       
        PaymentOrganisationAsProvider = 0,
        PaymentOrganisationFields = 1
    }
}
