using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Api.External.V4.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VariationReason
    {
        AuthorityFieldUpdated,

        EstablishmentNumberFieldUpdated,

        DfeEstablishmentNumberFieldUpdated,

        NameFieldUpdated,

        LACodeFieldUpdated,

        LegalNameFieldUpdated,

        TrustCodeFieldUpdated,

        FundingUpdated,

        ProfilingUpdated,

        URNFieldUpdated,

        CompaniesHouseNumberFieldUpdated,

        GroupIDFieldUpdated,

        RSCRegionCodeFieldUpdated,

        RSCRegionNameFieldUpdated,

        GovernmentOfficeRegionCodeFieldUpdated,

        GovernmentOfficeRegionNameFieldUpdated,

        DistrictCodeFieldUpdated,

        DistrictNameFieldUpdated,

        WardCodeFieldUpdated,

        WardNameFieldUpdated,

        CensusWardCodeFieldUpdated,

        CensusWardNameFieldUpdated,

        MiddleSuperOutputAreaCodeFieldUpdated,

        MiddleSuperOutputAreaNameFieldUpdated,

        LowerSuperOutputAreaCodeFieldUpdated,

        LowerSuperOutputAreaNameFieldUpdated,

        ParliamentaryConstituencyCodeFieldUpdated,

        ParliamentaryConstituencyNameFieldUpdated,

        CountryCodeFieldUpdated,

        CountryNameFieldUpdated
    }
}
