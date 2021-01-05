using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Specifications
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FundingStructureType
    {
        FundingLine,
        Calculation
    }
}