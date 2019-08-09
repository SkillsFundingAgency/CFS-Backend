using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Services.Publishing
{
    /// <summary>
    /// The reason for the groupig. Is it paid based on this grouping, or just informational.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GroupingReason
    {
        /// <summary>
        /// Paid in this way.
        /// </summary>
        Payment,

        /// <summary>
        /// Informational only.
        /// </summary>
        Information,

        /// <summary>
        /// Contracting
        /// </summary>
        Contracting,
    }
}
