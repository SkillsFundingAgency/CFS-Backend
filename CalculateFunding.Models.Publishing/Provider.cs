using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    public class Provider
    {
        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("trustStatus")]
        public ProviderTrustStatus TrustStatus { get; set; }

        [JsonProperty("name")]
        [VariationReasonValue(VariationReason.NameFieldUpdated)]
        public string Name { get; set; }

        [JsonProperty("urn")]
        [VariationReasonValue(VariationReason.URNFieldUpdated)]
        public string URN { get; set; }

        [JsonProperty("ukprn")]
        public string UKPRN { get; set; }

        [JsonProperty("upin")]
        public string UPIN { get; set; }

        [JsonProperty("establishmentNumber")]
        [VariationReasonValue(VariationReason.EstablishmentNumberFieldUpdated)]
        public string EstablishmentNumber { get; set; }

        [JsonProperty("dfeEstablishmentNumber")]
        [VariationReasonValue(VariationReason.DfeEstablishmentNumberFieldUpdated)]
        public string DfeEstablishmentNumber { get; set; }

        [JsonProperty("authority")]
        [VariationReasonValue(VariationReason.AuthorityFieldUpdated)]
        public string Authority { get; set; }

        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [JsonProperty("providerSubType")]
        public string ProviderSubType { get; set; }

        [JsonProperty("dateOpened")]
        public DateTimeOffset? DateOpened { get; set; }

        [JsonProperty("dateClosed")]
        public DateTimeOffset? DateClosed { get; set; }

        [JsonProperty("providerProfileIdType")]
        public string ProviderProfileIdType { get; set; }

        [JsonProperty("laCode")]
        [VariationReasonValue(VariationReason.LACodeFieldUpdated)]
        public string LACode { get; set; }

        [JsonProperty("navVendorNo")]
        public string NavVendorNo { get; set; }

        [JsonProperty("crmAccountId")]
        public string CrmAccountId { get; set; }

        [JsonProperty("legalName")]
        [VariationReasonValue(VariationReason.LegalNameFieldUpdated)]
        public string LegalName { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("phaseOfEducation")]
        public string PhaseOfEducation { get; set; }

        [JsonProperty("reasonEstablishmentOpened")]
        public string ReasonEstablishmentOpened { get; set; }

        [JsonProperty("reasonEstablishmentClosed")]
        public string ReasonEstablishmentClosed { get; set; }

        [JsonProperty("successor")]
        public string Successor { get; set; }
        
        [JsonProperty("successors")]
        public IEnumerable<string> Successors { get; set; }

        public IEnumerable<string> GetSuccessors()
        {
            if (Successors == null && Successor.IsNullOrWhitespace())
            {
                return ArraySegment<string>.Empty;
            }

            if (Successors != null)
            {
                return Successors;
            }
            
            return new[]
            {
                Successor
            };
        }

        [JsonProperty("trustName")]
        public string TrustName { get; set; }

        [JsonProperty("trustCode")]
        [VariationReasonValue(VariationReason.TrustCodeFieldUpdated)]
        public string TrustCode { get; set; }

        [JsonProperty("town")]
        public string Town { get; set; }

        [JsonProperty("postcode")]
        public string Postcode { get; set; }

        [JsonProperty("companiesHouseNumber")]
        [VariationReasonValue(VariationReason.CompaniesHouseNumberFieldUpdated)]
        public string CompaniesHouseNumber { get; set; }

        [JsonProperty("groupIdNumber")]
        [VariationReasonValue(VariationReason.GroupIDFieldUpdated)]
        public string GroupIdNumber { get; set; }

        [JsonProperty("rscRegionName")]
        [VariationReasonValue(VariationReason.RSCRegionNameFieldUpdated)]
        public string RscRegionName { get; set; }

        [JsonProperty("rscRegionCode")]
        [VariationReasonValue(VariationReason.RSCRegionCodeFieldUpdated)]
        public string RscRegionCode { get; set; }

        [JsonProperty("governmentOfficeRegionName")]
        [VariationReasonValue(VariationReason.GovernmentOfficeRegionNameFieldUpdated)]
        public string GovernmentOfficeRegionName { get; set; }

        [JsonProperty("governmentOfficeRegionCode")]
        [VariationReasonValue(VariationReason.GovernmentOfficeRegionCodeFieldUpdated)]
        public string GovernmentOfficeRegionCode { get; set; }

        [JsonProperty("districtName")]
        [VariationReasonValue(VariationReason.DistrictNameFieldUpdated)]
        public string DistrictName { get; set; }

        [JsonProperty("districtCode")]
        [VariationReasonValue(VariationReason.DistrictCodeFieldUpdated)]
        public string DistrictCode { get; set; }

        [JsonProperty("wardName")]
        [VariationReasonValue(VariationReason.WardNameFieldUpdated)]
        public string WardName { get; set; }

        [JsonProperty("wardCode")]
        [VariationReasonValue(VariationReason.WardCodeFieldUpdated)]
        public string WardCode { get; set; }

        [JsonProperty("censusWardName")]
        [VariationReasonValue(VariationReason.CensusWardNameFieldUpdated)]
        public string CensusWardName { get; set; }

        [JsonProperty("censusWardCode")]
        [VariationReasonValue(VariationReason.CensusWardCodeFieldUpdated)]
        public string CensusWardCode { get; set; }

        [JsonProperty("middleSuperOutputAreaName")]
        [VariationReasonValue(VariationReason.MiddleSuperOutputAreaNameFieldUpdated)]
        public string MiddleSuperOutputAreaName { get; set; }

        [JsonProperty("middleSuperOutputAreaCode")]
        [VariationReasonValue(VariationReason.MiddleSuperOutputAreaCodeFieldUpdated)]
        public string MiddleSuperOutputAreaCode { get; set; }

        [JsonProperty("lowerSuperOutputAreaName")]
        [VariationReasonValue(VariationReason.LowerSuperOutputAreaNameFieldUpdated)]
        public string LowerSuperOutputAreaName { get; set; }

        [JsonProperty("lowerSuperOutputAreaCode")]
        [VariationReasonValue(VariationReason.LowerSuperOutputAreaCodeFieldUpdated)]
        public string LowerSuperOutputAreaCode { get; set; }

        [JsonProperty("parliamentaryConstituencyName")]
        [VariationReasonValue(VariationReason.ParliamentaryConstituencyNameFieldUpdated)]
        public string ParliamentaryConstituencyName { get; set; }

        [JsonProperty("parliamentaryConstituencyCode")]
        [VariationReasonValue(VariationReason.ParliamentaryConstituencyCodeFieldUpdated)]
        public string ParliamentaryConstituencyCode { get; set; }

        [JsonProperty("countryCode")]
        [VariationReasonValue(VariationReason.CountryCodeFieldUpdated)]
        public string CountryCode { get; set; }

        [JsonProperty("countryName")]
        [VariationReasonValue(VariationReason.CountryNameFieldUpdated)]
        public string CountryName { get; set; }

        [JsonProperty("localGovernmentGroupTypeCode")]
        public string LocalGovernmentGroupTypeCode { get; set; }

        [JsonProperty("localGovernmentGroupTypeName")]
        public string LocalGovernmentGroupTypeName { get; set; }

        [JsonProperty("paymentOrganisationIdentifier")]
        [VariationReasonValue(VariationReason.PaymentOrganisationIdentifierFieldUpdated)]
        public string PaymentOrganisationIdentifier { get; set; }

        [JsonProperty("paymentOrganisationName")]
        [VariationReasonValue(VariationReason.PaymentOrganisationNameFieldUpdated)]
        public string PaymentOrganisationName { get; set; }
    }
}
