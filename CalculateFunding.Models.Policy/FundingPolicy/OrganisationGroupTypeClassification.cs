using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.FundingPolicy
{
    /// <summary>
    /// Valid list of organisation group categories.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrganisationGroupTypeClassification
    {
        /// <summary>
        /// Legal entity
        /// </summary>
        LegalEntity,

        /// <summary>
        /// Geographical boundary
        /// </summary>
        GeographicalBoundary,
    }
}
