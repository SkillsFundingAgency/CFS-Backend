using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Publishing
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VariationReason
    {
        [EnumMember(Value = nameof(AuthorityFieldUpdated))]
        AuthorityFieldUpdated,

        [EnumMember(Value = nameof(EstablishmentNumberFieldUpdated))]
        EstablishmentNumberFieldUpdated,

        [EnumMember(Value = nameof(DfeEstablishmentNumberFieldUpdated))]
        DfeEstablishmentNumberFieldUpdated,

        [EnumMember(Value = nameof(NameFieldUpdated))]
        NameFieldUpdated,

        [EnumMember(Value = nameof(LACodeFieldUpdated))]
        LACodeFieldUpdated,

        [EnumMember(Value = nameof(LegalNameFieldUpdated))]
        LegalNameFieldUpdated,

        [EnumMember(Value = nameof(TrustCodeFieldUpdated))]
        TrustCodeFieldUpdated,

        [EnumMember(Value = nameof(FundingUpdated))]
        FundingUpdated,

        [EnumMember(Value = nameof(ProfilingUpdated))]
        ProfilingUpdated,

        [EnumMember(Value = nameof(CalculationValuesUpdated))]
        CalculationValuesUpdated,

        [EnumMember(Value = nameof(URNFieldUpdated))]
        URNFieldUpdated,

        [EnumMember(Value = nameof(CompaniesHouseNumberFieldUpdated))]
        CompaniesHouseNumberFieldUpdated,

        [EnumMember(Value = nameof(GroupIDFieldUpdated))]
        GroupIDFieldUpdated,

        [EnumMember(Value = nameof(RSCRegionCodeFieldUpdated))]
        RSCRegionCodeFieldUpdated,

        [EnumMember(Value = nameof(RSCRegionNameFieldUpdated))]
        RSCRegionNameFieldUpdated,

        [EnumMember(Value = nameof(GovernmentOfficeRegionCodeFieldUpdated))]
        GovernmentOfficeRegionCodeFieldUpdated,

        [EnumMember(Value = nameof(GovernmentOfficeRegionNameFieldUpdated))]
        GovernmentOfficeRegionNameFieldUpdated,

        [EnumMember(Value = nameof(DistrictCodeFieldUpdated))]
        DistrictCodeFieldUpdated,

        [EnumMember(Value = nameof(DistrictNameFieldUpdated))]
        DistrictNameFieldUpdated,

        [EnumMember(Value = nameof(WardCodeFieldUpdated))]
        WardCodeFieldUpdated,

        [EnumMember(Value = nameof(WardNameFieldUpdated))]
        WardNameFieldUpdated,

        [EnumMember(Value = nameof(CensusWardCodeFieldUpdated))]
        CensusWardCodeFieldUpdated,

        [EnumMember(Value = nameof(CensusWardNameFieldUpdated))]
        CensusWardNameFieldUpdated,

        [EnumMember(Value = nameof(MiddleSuperOutputAreaCodeFieldUpdated))]
        MiddleSuperOutputAreaCodeFieldUpdated,

        [EnumMember(Value = nameof(MiddleSuperOutputAreaNameFieldUpdated))]
        MiddleSuperOutputAreaNameFieldUpdated,

        [EnumMember(Value = nameof(LowerSuperOutputAreaCodeFieldUpdated))]
        LowerSuperOutputAreaCodeFieldUpdated,

        [EnumMember(Value = nameof(LowerSuperOutputAreaNameFieldUpdated))]
        LowerSuperOutputAreaNameFieldUpdated,

        [EnumMember(Value = nameof(ParliamentaryConstituencyCodeFieldUpdated))]
        ParliamentaryConstituencyCodeFieldUpdated,

        [EnumMember(Value = nameof(ParliamentaryConstituencyNameFieldUpdated))]
        ParliamentaryConstituencyNameFieldUpdated,

        [EnumMember(Value = nameof(CountryCodeFieldUpdated))]
        CountryCodeFieldUpdated,

        [EnumMember(Value = nameof(CountryNameFieldUpdated))]
        CountryNameFieldUpdated,

        [EnumMember(Value = nameof(PaymentOrganisationIdentifierFieldUpdated))]
        PaymentOrganisationIdentifierFieldUpdated,

        [EnumMember(Value = nameof(PaymentOrganisationNameFieldUpdated))]
        PaymentOrganisationNameFieldUpdated,

        [EnumMember(Value = nameof(DateOpenedFieldUpdated))]
        DateOpenedFieldUpdated,

        [EnumMember(Value = nameof(DateClosedFieldUpdated))]
        DateClosedFieldUpdated,

        [EnumMember(Value = nameof(ProviderStatusFieldUpdated))]
        ProviderStatusFieldUpdated,

        [EnumMember(Value = nameof(PhaseOfEducationFieldUpdated))]
        PhaseOfEducationFieldUpdated,

        [EnumMember(Value = nameof(ReasonEstablishmentOpenedFieldUpdated))]
        ReasonEstablishmentOpenedFieldUpdated,

        [EnumMember(Value = nameof(ReasonEstablishmentClosedFieldUpdated))]
        ReasonEstablishmentClosedFieldUpdated,

        [EnumMember(Value = nameof(TrustStatusFieldUpdated))]
        TrustStatusFieldUpdated,

        [EnumMember(Value = nameof(TownFieldUpdated))]
        TownFieldUpdated,

        [EnumMember(Value = nameof(PostcodeFieldUpdated))]
        PostcodeFieldUpdated,

        [EnumMember(Value = nameof(TemplateUpdated))]
        TemplateUpdated,

        [EnumMember(Value = nameof(FundingSchemaUpdated))]
        FundingSchemaUpdated
    }
}
