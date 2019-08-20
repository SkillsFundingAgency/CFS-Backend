using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Obsoleted
{
    [Obsolete]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FundingRoute
    {
        Provider,
        LA
    }
}
