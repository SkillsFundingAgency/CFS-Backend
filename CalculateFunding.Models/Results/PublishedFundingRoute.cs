using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PublishedFundingRoute
    {
        Provider,
        LA
    }
}
