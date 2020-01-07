using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    /// Valid list of calculation types.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FundingCalculationType
    {
        /// <summary>
        /// A monetry value, not multipled by anything.
        /// </summary>
        Cash,

        /// <summary>
        /// Cash per paid X.
        /// </summary>
        Rate,

        /// <summary>
        /// Number of pupils.
        /// </summary>
        PupilNumber,

        /// <summary>
        /// A number between 0 and 1.
        /// </summary>
        Weighting,

        /// <summary>
        /// Work out eligibility (0 or 1).
        /// </summary>
        Scope,

        /// <summary>
        /// Informational information only.
        /// </summary>
        Information,

        /// <summary>
        /// 
        /// </summary>
        Drilldown,

        /// <summary>
        /// 
        /// </summary>
        PerPupilFunding,

        /// <summary>
        /// 
        /// </summary>
        LumpSum,

        /// <summary>
        /// 
        /// </summary>
        ProviderLedFunding,
    }
}
