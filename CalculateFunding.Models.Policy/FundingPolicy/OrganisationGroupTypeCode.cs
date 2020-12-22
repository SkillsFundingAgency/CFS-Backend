using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Policy.FundingPolicy
{
    /// <summary>
    /// Valid list of organisation group types.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrganisationGroupTypeCode
    {
        /// <summary>
        /// Local Authority (e.g. Warwickshire).
        /// </summary>
        LocalAuthority,

        /// <summary>
        /// Academy Trust (e.g. Star Foundation).
        /// </summary>
        AcademyTrust,

        /// <summary>
        /// Regional Schools Commissioner Region (e.g. Lancashire and West Yorkshire).
        /// </summary>
        RSCRegion,

        /// <summary>
        /// Government Office Region, (e.g. North West).
        /// </summary>
        GovernmentOfficeRegion,

        /// <summary>
        /// District (e.g. Hyndburn).
        /// </summary>
        District,

        /// <summary>
        /// Ward (e.g. Milnshaw).
        /// </summary>
        Ward,

        /// <summary>
        /// Census Ward.
        /// </summary>
        CensusWard,

        /// <summary>
        /// Middle Super Output Area (e.g. Mansfield 002).
        /// </summary>
        MiddleSuperOutputArea,

        /// <summary>
        /// Lower Super Output Area (e.g. Mansfield 002A).
        /// </summary>
        LowerSuperOutputArea,

        /// <summary>
        /// Parlimentry constituency (e.g. Mansfield).
        /// </summary>
        ParliamentaryConstituency,

        /// <summary>
        /// Provider
        /// </summary>
        Provider,

        /// <summary>
        /// Region
        /// </summary>
        Region,

        /// <summary>
        /// Country eg GB
        /// </summary>
        Country,

        /// <summary>
        /// Local Government Group
        /// </summary>
        LocalGovernmentGroup,

        /// <summary>
        /// Local Authority - School Six Forms
        /// </summary>
        LocalAuthoritySsf,

        /// <summary>
        /// Local Authority - Maintained Special Schools
        /// </summary>

        LocalAuthorityMss,

        /// <summary>
        /// Local Authority - Maintained Schools
        /// </summary>
        LocalAuthorityMaintained,
    }
}
