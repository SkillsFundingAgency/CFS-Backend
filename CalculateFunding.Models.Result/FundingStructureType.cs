using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Result
{
    [Obsolete]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FundingStructureType
    {
        FundingLine,
        Calculation
    }
}
