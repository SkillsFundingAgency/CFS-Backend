using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Policy
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FundingStructureType
    {
        FundingLine,
        Calculation
    }
}