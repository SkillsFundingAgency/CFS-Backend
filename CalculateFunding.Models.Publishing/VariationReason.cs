using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CalculateFunding.Models.Publishing
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VariationReason
    {
        [EnumMember(Value = nameof(AuthorityFieldUpdated))]
        [Display(Name = "Local authority name updated")]
        [SqlConstantId(1)]
        AuthorityFieldUpdated,

        [Display(Name = "Establishment number updated")]
        [SqlConstantId(2)]
        [EnumMember(Value = nameof(EstablishmentNumberFieldUpdated))]
        EstablishmentNumberFieldUpdated,

        [Display(Name = "DfE establishment number updated")]
        [SqlConstantId(3)]
        [EnumMember(Value = nameof(DfeEstablishmentNumberFieldUpdated))]
        DfeEstablishmentNumberFieldUpdated,

        [Display(Name = "Provider name updated")]
        [SqlConstantId(4)]
        [EnumMember(Value = nameof(NameFieldUpdated))]
        NameFieldUpdated,

        [Display(Name = "Local authority code updated")]
        [SqlConstantId(5)]
        [EnumMember(Value = nameof(LACodeFieldUpdated))]
        LACodeFieldUpdated,

        [Display(Name = "Legal name updated")]
        [SqlConstantId(6)]
        [EnumMember(Value = nameof(LegalNameFieldUpdated))]
        LegalNameFieldUpdated,

        [Display(Name = "Trust code updated")]
        [SqlConstantId(7)]
        [EnumMember(Value = nameof(TrustCodeFieldUpdated))]
        TrustCodeFieldUpdated,

        [Display(Name = "Funding value updated")]
        [SqlConstantId(8)]
        [EnumMember(Value = nameof(FundingUpdated))]
        FundingUpdated,

        [Display(Name = "Profiling updated")]
        [SqlConstantId(9)]
        [EnumMember(Value = nameof(ProfilingUpdated))]
        ProfilingUpdated,

        [Display(Name = "Calculatation values updated")]
        [SqlConstantId(10)]
        [EnumMember(Value = nameof(CalculationValuesUpdated))]
        CalculationValuesUpdated,

        [Display(Name = "URN updated")]
        [SqlConstantId(11)]
        [EnumMember(Value = nameof(URNFieldUpdated))]
        URNFieldUpdated,

        [Display(Name = "Companies house number updated")]
        [SqlConstantId(12)]
        [EnumMember(Value = nameof(CompaniesHouseNumberFieldUpdated))]
        CompaniesHouseNumberFieldUpdated,

        [Display(Name = "Group ID field updated")]
        [SqlConstantId(13)]
        [EnumMember(Value = nameof(GroupIDFieldUpdated))]
        GroupIDFieldUpdated,

        [Display(Name = "RSC region code updated")]
        [SqlConstantId(14)]
        [EnumMember(Value = nameof(RSCRegionCodeFieldUpdated))]
        RSCRegionCodeFieldUpdated,

        [Display(Name = "RSC region name updated")]
        [SqlConstantId(15)]
        [EnumMember(Value = nameof(RSCRegionNameFieldUpdated))]
        RSCRegionNameFieldUpdated,

        [Display(Name = "Government office region code updated")]
        [SqlConstantId(16)]
        [EnumMember(Value = nameof(GovernmentOfficeRegionCodeFieldUpdated))]
        GovernmentOfficeRegionCodeFieldUpdated,

        [Display(Name = "Government office region name updated")]
        [SqlConstantId(17)]
        [EnumMember(Value = nameof(GovernmentOfficeRegionNameFieldUpdated))]
        GovernmentOfficeRegionNameFieldUpdated,

        [EnumMember(Value = nameof(DistrictCodeFieldUpdated))]
        DistrictCodeFieldUpdated,

        [Display(Name = "District name updated")]
        [SqlConstantId(18)]
        [EnumMember(Value = nameof(DistrictNameFieldUpdated))]
        DistrictNameFieldUpdated,

        [Display(Name = "Ward code updated")]
        [SqlConstantId(19)]
        [EnumMember(Value = nameof(WardCodeFieldUpdated))]
        WardCodeFieldUpdated,

        [Display(Name = "Ward name updated")]
        [SqlConstantId(20)]
        [EnumMember(Value = nameof(WardNameFieldUpdated))]
        WardNameFieldUpdated,

        [Display(Name = "Census ward code updated")]
        [SqlConstantId(21)]
        [EnumMember(Value = nameof(CensusWardCodeFieldUpdated))]
        CensusWardCodeFieldUpdated,

        [Display(Name = "Census ward name updated")]
        [SqlConstantId(22)]
        [EnumMember(Value = nameof(CensusWardNameFieldUpdated))]
        CensusWardNameFieldUpdated,

        [Display(Name = "Middle super output area code updated")]
        [SqlConstantId(23)]
        [EnumMember(Value = nameof(MiddleSuperOutputAreaCodeFieldUpdated))]
        MiddleSuperOutputAreaCodeFieldUpdated,

        [Display(Name = "Middle suport output area name updated")]
        [SqlConstantId(24)]
        [EnumMember(Value = nameof(MiddleSuperOutputAreaNameFieldUpdated))]
        MiddleSuperOutputAreaNameFieldUpdated,

        [Display(Name = "Lower super output area code updated")]
        [SqlConstantId(25)]
        [EnumMember(Value = nameof(LowerSuperOutputAreaCodeFieldUpdated))]
        LowerSuperOutputAreaCodeFieldUpdated,

        [Display(Name = "Lower super output area name updated")]
        [SqlConstantId(26)]
        [EnumMember(Value = nameof(LowerSuperOutputAreaNameFieldUpdated))]
        LowerSuperOutputAreaNameFieldUpdated,

        [Display(Name = "Parliamentary constituency code updated")]
        [SqlConstantId(27)]
        [EnumMember(Value = nameof(ParliamentaryConstituencyCodeFieldUpdated))]
        ParliamentaryConstituencyCodeFieldUpdated,

        [Display(Name = "Parliamentary constituency name updated")]
        [SqlConstantId(28)]
        [EnumMember(Value = nameof(ParliamentaryConstituencyNameFieldUpdated))]
        ParliamentaryConstituencyNameFieldUpdated,

        [Display(Name = "Country code updated")]
        [SqlConstantId(29)]
        [EnumMember(Value = nameof(CountryCodeFieldUpdated))]
        CountryCodeFieldUpdated,

        [Display(Name = "Country name updated")]
        [SqlConstantId(30)]
        [EnumMember(Value = nameof(CountryNameFieldUpdated))]
        CountryNameFieldUpdated,

        [Display(Name = "Payment organisation identifer updated")]
        [SqlConstantId(31)]
        [EnumMember(Value = nameof(PaymentOrganisationIdentifierFieldUpdated))]
        PaymentOrganisationIdentifierFieldUpdated,

        [Display(Name = "Payment organisation name updated")]
        [SqlConstantId(32)]
        [EnumMember(Value = nameof(PaymentOrganisationNameFieldUpdated))]
        PaymentOrganisationNameFieldUpdated,

        [Display(Name = "Date opened updated")]
        [SqlConstantId(33)]
        [EnumMember(Value = nameof(DateOpenedFieldUpdated))]
        DateOpenedFieldUpdated,

        [Display(Name = "Date closed updated")]
        [SqlConstantId(34)]
        [EnumMember(Value = nameof(DateClosedFieldUpdated))]
        DateClosedFieldUpdated,

        [Display(Name = "Provider status updated")]
        [SqlConstantId(35)]
        [EnumMember(Value = nameof(ProviderStatusFieldUpdated))]
        ProviderStatusFieldUpdated,

        [Display(Name = "Phase of education updated")]
        [SqlConstantId(36)]
        [EnumMember(Value = nameof(PhaseOfEducationFieldUpdated))]
        PhaseOfEducationFieldUpdated,

        [Display(Name = "Reason estabishment opened updated")]
        [SqlConstantId(37)]
        [EnumMember(Value = nameof(ReasonEstablishmentOpenedFieldUpdated))]
        ReasonEstablishmentOpenedFieldUpdated,

        [Display(Name = "Reason establishment closed updated")]
        [SqlConstantId(38)]
        [EnumMember(Value = nameof(ReasonEstablishmentClosedFieldUpdated))]
        ReasonEstablishmentClosedFieldUpdated,

        [Display(Name = "Trust status updated")]
        [SqlConstantId(39)]
        [EnumMember(Value = nameof(TrustStatusFieldUpdated))]
        TrustStatusFieldUpdated,

        [Display(Name = "Town updated")]
        [SqlConstantId(40)]
        [EnumMember(Value = nameof(TownFieldUpdated))]
        TownFieldUpdated,

        [Display(Name = "Postcode updated")]
        [SqlConstantId(41)]
        [EnumMember(Value = nameof(PostcodeFieldUpdated))]
        PostcodeFieldUpdated,

        [Display(Name = "Template version updated")]
        [SqlConstantId(42)]
        [EnumMember(Value = nameof(TemplateUpdated))]
        TemplateUpdated,

        [Display(Name = "Funding schema updated")]
        [SqlConstantId(43)]
        [EnumMember(Value = nameof(FundingSchemaUpdated))]
        FundingSchemaUpdated,

        [EnumMember(Value = nameof(DistributionProfileUpdated))]
        DistributionProfileUpdated,

        [Display(Name = "Provider changed from indicative to live")]
        [SqlConstantId(44)]
        [EnumMember(Value = nameof(IndicativeToLive))]
        IndicativeToLive,
    }
}
