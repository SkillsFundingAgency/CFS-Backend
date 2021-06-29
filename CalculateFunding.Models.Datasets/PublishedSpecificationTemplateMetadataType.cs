using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Datasets
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PublishedSpecificationTemplateMetadataType
    {
        Calculation,
        FundingLine
    }
}
