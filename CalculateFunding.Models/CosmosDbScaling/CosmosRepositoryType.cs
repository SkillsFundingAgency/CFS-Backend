using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.CosmosDbScaling
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CosmosRepositoryType
    {
        CalculationProviderResults,
        ProviderSourceDatasets,
        PublishedProviderResults
    }
}
