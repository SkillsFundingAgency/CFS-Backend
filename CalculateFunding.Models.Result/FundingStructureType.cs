using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Result
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FundingStructureType
    {
        FundingLine,
        Calculation
    }
}