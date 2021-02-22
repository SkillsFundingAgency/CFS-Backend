using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Services.Profiling.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RoundingStrategy
    {
        /// <summary>
        /// Round up to the whole pound, based on the value midpoint rounding at 2 decimal places
        /// </summary>
        RoundUp,

        /// <summary>
        /// Floor the pence, the move the rest to the last period
        /// </summary>
        RoundDown,

        /// <summary>
        /// Round at the 2 decimal place midpoint for whole pence
        /// </summary>
        MidpointTwoDecimalPlaces
    }
}
