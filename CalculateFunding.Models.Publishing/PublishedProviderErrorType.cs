using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace CalculateFunding.Models.Publishing
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PublishedProviderErrorType
    {
        Undefined = 0,
        FundingLineValueProfileMismatch,
        TrustIdMismatch,
        PostPaymentOutOfScopeProvider,
        ProfilingConsistencyCheckFailure
    }
}