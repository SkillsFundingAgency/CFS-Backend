using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.FundingPolicy
{
    /// <summary>
    /// Valid list of organisation group types.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrganisationGroupTypeIdentifier
    {
        /// <summary>
        /// UKPRN
        /// </summary>
        [EnumMember(Value = "UKPRN")]
        UKPRN,

        /// <summary>
        /// LACode
        /// </summary>
        [EnumMember(Value = "LACode")]
        LACode,

        /// <summary>
        /// UPIN
        /// </summary>
        [EnumMember(Value = "UPIN")]
        UPIN,

        /// <summary>
        /// URN
        /// </summary>
        [EnumMember(Value = "URN")]
        URN,

        /// <summary>
        /// UID
        /// </summary>
        [EnumMember(Value = "UID")]
        UID,

        /// <summary>
        /// CompaniesHouseNumber
        /// </summary>
        [EnumMember(Value = "CompaniesHouseNumber")]
        CompaniesHouseNumber,

        /// <summary>
        /// GroupID
        /// </summary>
        [EnumMember(Value = "GroupID")]
        GroupId,

        /// <summary>
        /// RSCRegionCode
        /// </summary>
        [EnumMember(Value = "RSCRegionCode")]
        RSCRegionCode,

        /// <summary>
        /// GovernmentOfficeRegionCode
        /// </summary>
        [EnumMember(Value = "GovernmentOfficeRegionCode")]
        GovernmentOfficeRegionCode,

        /// <summary>
        /// DistrictCode
        /// </summary>
        [EnumMember(Value = "DistrictCode")]
        DistrictCode,

        /// <summary>
        /// WardCode
        /// </summary>
        [EnumMember(Value = "WardCode")]
        WardCode,

        /// <summary>
        /// CensusWardCode
        /// </summary>
        [EnumMember(Value = "CensusWardCode")]
        CensusWardCode,

        /// <summary>
        /// MiddleSuperOutputAreaCode
        /// </summary>
        [EnumMember(Value = "MiddleSuperOutputAreaCode")]
        MiddleSuperOutputAreaCode,

        /// <summary>
        /// LowerSuperOutputAreaCode
        /// </summary>
        [EnumMember(Value = "LowerSuperOutputAreaCode")]
        LowerSuperOutputAreaCode,

        /// <summary>
        /// ParliamentaryConstituencyCode
        /// </summary>
        [EnumMember(Value = "ParliamentaryConstituencyCode")]
        ParliamentaryConstituencyCode,

        /// <summary>
        /// DfeNumber
        /// </summary>
        [EnumMember(Value = "DfeNumber")]
        DfeNumber,

        /// <summary>
        /// AcademyTrustCode
        /// </summary>
        [EnumMember(Value = "AcademyTrustCode")]
        AcademyTrustCode,

        /// <summary>
        /// CountryCode
        /// </summary>
        [EnumMember(Value = "CountryCode")]
        CountryCode,

        /// <summary>
        /// LocalAuthorityClassificationTypeCode
        /// </summary>
        [EnumMember(Value = "LocalAuthorityClassificationTypeCode")]
        LocalAuthorityClassificationTypeCode,
    }
}
