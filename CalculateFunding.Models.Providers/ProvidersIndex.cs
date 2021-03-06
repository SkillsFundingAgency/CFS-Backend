﻿using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Providers
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "providersindex", IndexerName = "providersindexer")]
    public class ProvidersIndex
    {
        [Key]
        [JsonProperty("id")]
        public string Id => $"{ProviderVersionId}_{UKPRN}";

        [IsFilterable]
        [IsFacetable]
        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }

        [IsFilterable]
        [IsFacetable]
        [IsSearchable]
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [IsSearchable]
        [JsonProperty("name")]
        public string Name { get; set; }

        [IsSearchable]
        [JsonProperty("urn")]
        public string URN { get; set; }

        [IsSearchable]
        [JsonProperty("ukprn")]
        public string UKPRN { get; set; }

        [IsSearchable]
        [JsonProperty("upin")]
        public string UPIN { get; set; }

        [JsonProperty("establishmentNumber")]
        public string EstablishmentNumber { get; set; }

        [JsonProperty("dfeEstablishmentNumber")]
        public string DfeEstablishmentNumber { get; set; }

        [IsFacetable]
        [IsFilterable]
        [JsonProperty("authority")]
        public string Authority { get; set; }

        [IsFacetable]
        [IsFilterable]
        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [IsFacetable]
        [IsFilterable]
        [JsonProperty("providerSubType")]
        public string ProviderSubType { get; set; }

        [JsonProperty("dateOpened")]
        public DateTimeOffset? DateOpened { get; set; }

        [JsonProperty("dateClosed")]
        public DateTimeOffset? DateClosed { get; set; }

        [JsonProperty("providerProfileIdType")]
        public string ProviderProfileIdType { get; set; }

        [JsonProperty("laCode")]
        public string LaCode { get; set; }

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

        [JsonProperty("trustStatus")]
        public string TrustStatus { get; set; }

        [JsonProperty("trustName")]
        public string TrustName { get; set; }

        [JsonProperty("trustCode")]
        public string TrustCode { get; set; }

        [JsonProperty("town")]
        public string Town { get; set; }

        [JsonProperty("postcode")]
        public string Postcode { get; set; }

        [IsFilterable]
        [JsonProperty("localAuthorityName")]
        public string LocalAuthorityName { get; set; }

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
    }
}
