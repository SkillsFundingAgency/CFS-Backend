using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.FundingPolicy
{
    /// <summary>
    /// Valid list of the different unique ways to identifier an organisation.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrganisationIdentifierType
    {
        /// <summary>
        /// UK Provider Reference Number - the unique identifier allocated to providers by the UK Register of Learning Providers (UKRLP) - 8 digits.
        /// </summary>
        UKPRN,

        /// <summary>
        /// The code of the local education authority.
        /// </summary>
        LACode,

        /// <summary>
        ///  Unique provider identification number. A 6 digit number to represent a provider.
        /// </summary>
        UPIN,

        /// <summary>
        /// Unique Reference Number.
        /// </summary>
        URN,

        /// <summary>
        /// Unique Identifier (used for MATs).
        /// </summary>
        UID,

        /// <summary>
        /// The company number on Companies House.
        /// </summary>
        CompaniesHouseNumber,

        /// <summary>
        /// A code given to MATs.
        /// </summary>
        GroupID,

        /// <summary>
        /// Name of the RSC Region.
        /// </summary>
        RSCRegionName,

        /// <summary>
        /// Name of the government office.
        /// </summary>
        GovernmentOfficeRegion,

        /// <summary>
        /// Name of the district.
        /// </summary>
        DistrictName,

        /// <summary>
        /// Name of the ward.
        /// </summary>
        WardName,

        /// <summary>
        /// Name of the census ward.
        /// </summary>
        CensusWardName,

        /// <summary>
        /// Middle Super Output Area code.
        /// </summary>
        MiddleSuperOutputAreaCode,

        /// <summary>
        /// Lower Super Output Area code.
        /// </summary>
        LowerSuperOutputAreaCode,

        /// <summary>
        /// Name of the parlimentary constituency.
        /// </summary>
        ParliamentaryConstituencyName,

        /// <summary>
        /// The DfE number.
        /// </summary>
        DfeNumber,

        /// <summary>
        /// Academy trust code
        /// </summary>
        AcademyTrustCode,
    }
}
