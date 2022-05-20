using CalculateFunding.Models.ProviderLegacy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Providers
{
    public class Provider
    {
        [JsonProperty("providerVersionId_providerId")]
        public string ProviderVersionIdProviderId;

        [JsonProperty("providerVersionId")]
        public string ProviderVersionId;

        [JsonProperty("providerId")]
        public string ProviderId;

        [JsonProperty("trustStatus")]
        public string TrustStatusViewModelString;

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("urn")]
        public string URN { get; set; }

        [JsonProperty("ukPrn")]
        public string UKPRN { get; set; }

        [JsonProperty("upin")]
        public string UPIN { get; set; }

        [JsonProperty("establishmentNumber")]
        public string EstablishmentNumber { get; set; }

        [JsonProperty("dfeEstablishmentNumber")]
        public string DfeEstablishmentNumber { get; set; }

        [JsonProperty("authority")]
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
        public string LACode { get; set; }
        [JsonProperty("laOrg")]
        public string LAOrg { get; set; }

        [JsonProperty("navVendorNo")]
        public string NavVendorNo { get; set; }

        [JsonProperty("crmAccountId")]
        public string CrmAccountId { get; set; }

        [JsonProperty("legalName")]
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

        [JsonProperty("town")]
        public string Town { get; set; }

        [JsonProperty("postcode")]
        public string Postcode { get; set; }

        [JsonProperty("trustName")]
        public string TrustName { get; set; }

        [JsonProperty("trustCode")]
        public string TrustCode { get; set; }

        [JsonProperty("companiesHouseNumber")]
        public string CompaniesHouseNumber { get; set; }

        [JsonProperty("groupIdNumber")]
        public string GroupIdNumber { get; set; }

        [JsonProperty("rscRegionName")]
        public string RscRegionName { get; set; }

        [JsonProperty("rscRegionCode")]
        public string RscRegionCode { get; set; }

        [JsonProperty("governmentOfficeRegionName")]
        public string GovernmentOfficeRegionName { get; set; }

        [JsonProperty("governmentOfficeRegionCode")]
        public string GovernmentOfficeRegionCode { get; set; }

        [JsonProperty("districtName")]
        public string DistrictName { get; set; }

        [JsonProperty("districtCode")]
        public string DistrictCode { get; set; }

        [JsonProperty("wardName")]
        public string WardName { get; set; }

        [JsonProperty("wardCode")]
        public string WardCode { get; set; }

        [JsonProperty("censusWardName")]
        public string CensusWardName { get; set; }

        [JsonProperty("censusWardCode")]
        public string CensusWardCode { get; set; }

        [JsonProperty("middleSuperOutputAreaName")]
        public string MiddleSuperOutputAreaName { get; set; }

        [JsonProperty("middleSuperOutputAreaCode")]
        public string MiddleSuperOutputAreaCode { get; set; }

        [JsonProperty("lowerSuperOutputAreaName")]
        public string LowerSuperOutputAreaName { get; set; }

        [JsonProperty("lowerSuperOutputAreaCode")]
        public string LowerSuperOutputAreaCode { get; set; }

        [JsonProperty("parliamentaryConstituencyName")]
        public string ParliamentaryConstituencyName { get; set; }

        [JsonProperty("parliamentaryConstituencyCode")]
        public string ParliamentaryConstituencyCode { get; set; }

        [JsonProperty("londonRegionCode")]
        public string LondonRegionCode { get; set; }

        [JsonProperty("londonRegionName")]
        public string LondonRegionName { get; set; }

        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        [JsonProperty("countryName")]
        public string CountryName { get; set; }

        [JsonProperty("localGovernmentGroupTypeCode")]
        public string LocalGovernmentGroupTypeCode { get; set; }

        [JsonProperty("localGovernmentGroupTypeName")]
        public string LocalGovernmentGroupTypeName { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("locality")]
        public string Locality { get; set; }

        [JsonProperty("address3")]
        public string Address3 { get; set; }

        [JsonProperty("paymentOrganisationIdentifier")]
        public string PaymentOrganisationIdentifier { get; set; }

        [JsonProperty("paymentOrganisationName")]
        public string PaymentOrganisationName { get; set; }

        [JsonProperty("providerTypeCode")]
        public string ProviderTypeCode { get; set; }

        [JsonProperty("providerSubTypeCode")]
        public string ProviderSubTypeCode { get; set; }

        [JsonProperty("previousLACode")]
        public string PreviousLACode { get; set; }

        [JsonProperty("previousLAName")]
        public string PreviousLAName { get; set; }

        [JsonProperty("previousEstablishmentNumber")]
        public string PreviousEstablishmentNumber { get; set; }

        [JsonProperty("furtherEducationTypeCode")]
        public string FurtherEducationTypeCode { get; set; }

        [JsonProperty("furtherEducationTypeName")]
        public string FurtherEducationTypeName { get; set; }

        [JsonProperty("predecessors")]
        public IEnumerable<string> Predecessors { get; set; }

        [JsonProperty("successors")]
        public IEnumerable<string> Successors { get; set; }

        [JsonProperty("phaseOfEducationCode")]
        public string PhaseOfEducationCode { get; set; }

        [JsonProperty("statutoryLowAge")]
        public string StatutoryLowAge { get; set; }

        [JsonProperty("statutoryHighAge")]
        public string StatutoryHighAge { get; set; }

        [JsonProperty("officialSixthFormCode")]
        public string OfficialSixthFormCode { get; set; }

        [JsonProperty("officialSixthFormName")]
        public string OfficialSixthFormName { get; set; }

        [JsonProperty("previousLaCode")]
        public string PreviousLaCode { get; set; }

        [JsonProperty("previousLaName")]
        public string PreviousLaName { get; set; }

        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }

        [JsonProperty("reasonEstablishmentOpenedCode")]
        public string ReasonEstablishmentOpenedCode { get; set; }

        [JsonProperty("reasonEstablishmentClosedCode")]
        public string ReasonEstablishmentClosedCode { get; set; }

        [JsonIgnore]
        public TrustStatus TrustStatus
        {
            get
            {
                if (Enum.TryParse<TrustStatus>(TrustStatusViewModelString, true, out var result))
                {
                    return result;
                }

                return TrustStatus.NotApplicable;
            }
            set
            {
                TrustStatusViewModelString = value.ToString();
            }
        }
    }
}
