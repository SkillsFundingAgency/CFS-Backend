using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace CalculateFunding.Services.Profiling.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProfilePatternType
    {
        /// <summary>
        /// Profile pattern distribution through percentages
        /// </summary>
        Percent,

        /// <summary>
        /// Profile pattern distribution through template calculations
        /// </summary>
        Calculation,
    }
}
