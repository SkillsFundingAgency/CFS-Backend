using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Publishing
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VariationReason
    {
        [EnumMember(Value = "AuthorityFieldUpdated")]
        AuthorityFieldUpdated,

        [EnumMember(Value = "EstablishmentNumberFieldUpdated")]
        EstablishmentNumberFieldUpdated,

        [EnumMember(Value = "DfeEstablishmentNumberFieldUpdated")]
        DfeEstablishmentNumberFieldUpdated,

        [EnumMember(Value = "NameFieldUpdated")]
        NameFieldUpdated,

        [EnumMember(Value = "LACodeFieldUpdated")]
        LACodeFieldUpdated,

        [EnumMember(Value = "LegalNameFieldUpdated")]
        LegalNameFieldUpdated,

        [EnumMember(Value = "TrustCodeFieldUpdated")]
        TrustCodeFieldUpdated,

        [EnumMember(Value = "FundingUpdated")]
        FundingUpdated,

        [EnumMember(Value = "ProfilingUpdated")]
        ProfilingUpdated,

        [EnumMember(Value = "URNFieldUpdated")]
        URNFieldUpdated,

        [EnumMember(Value = "CompaniesHouseNumberFieldUpdated")]
        CompaniesHouseNumberFieldUpdated,

        [EnumMember(Value = "GroupIDFieldUpdated")]
        GroupIDFieldUpdated,

        [EnumMember(Value = "RSCRegionCodeFieldUpdated")]
        RSCRegionCodeFieldUpdated,

        [EnumMember(Value = "RSCRegionNameFieldUpdated")]
        RSCRegionNameFieldUpdated,

        [EnumMember(Value = "GovernmentOfficeRegionCodeFieldUpdated")]
        GovernmentOfficeRegionCodeFieldUpdated,

        [EnumMember(Value = "GovernmentOfficeRegionNameFieldUpdated")]
        GovernmentOfficeRegionNameFieldUpdated,

        [EnumMember(Value = "DistrictCodeFieldUpdated")]
        DistrictCodeFieldUpdated,

        [EnumMember(Value = "DistrictNameFieldUpdated")]
        DistrictNameFieldUpdated,

        [EnumMember(Value = "WardCodeFieldUpdated")]
        WardCodeFieldUpdated,

        [EnumMember(Value = "WardNameFieldUpdated")]
        WardNameFieldUpdated,

        [EnumMember(Value = "CensusWardCodeFieldUpdated")]
        CensusWardCodeFieldUpdated,

        [EnumMember(Value = "CensusWardNameFieldUpdated")]
        CensusWardNameFieldUpdated,

        [EnumMember(Value = "MiddleSuperOutputAreaCodeFieldUpdated")]
        MiddleSuperOutputAreaCodeFieldUpdated,

        [EnumMember(Value = "MiddleSuperOutputAreaNameFieldUpdated")]
        MiddleSuperOutputAreaNameFieldUpdated,

        [EnumMember(Value = "LowerSuperOutputAreaCodeFieldUpdated")]
        LowerSuperOutputAreaCodeFieldUpdated,

        [EnumMember(Value = "LowerSuperOutputAreaNameFieldUpdated")]
        LowerSuperOutputAreaNameFieldUpdated,

        [EnumMember(Value = "ParliamentaryConstituencyCodeFieldUpdated")]
        ParliamentaryConstituencyCodeFieldUpdated,

        [EnumMember(Value = "ParliamentaryConstituencyNameFieldUpdated")]
        ParliamentaryConstituencyNameFieldUpdated,

        [EnumMember(Value = "CountryCodeFieldUpdated")]
        CountryCodeFieldUpdated,

        [EnumMember(Value = "CountryNameFieldUpdated")]
        CountryNameFieldUpdated,
    }
}
