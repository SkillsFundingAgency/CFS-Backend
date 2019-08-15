using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Providers
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "providersindex")]
    public class ProvidersIndex
    {
        [Key]
        [JsonProperty("id")]
        public string Id => $"{ProviderVersionId}_{UKPRN}";

        [IsFilterable]
        [IsFacetable]
        [IsSearchable]
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
        [JsonProperty("ukPrn")]
        public string UKPRN { get; set; }

        [IsSearchable]
        [JsonProperty("upin")]
        public string UPIN { get; set; }

        [IsSearchable]
        [JsonProperty("establishmentNumber")]
        public string EstablishmentNumber { get; set; }

        [IsSearchable]
        [JsonProperty("dfeEstablishmentNumber")]
        public string DfeEstablishmentNumber { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [JsonProperty("authority")]
        public string Authority { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [JsonProperty("providerSubType")]
        public string ProviderSubType { get; set; }

        [JsonProperty("dateOpened")]
        public DateTimeOffset? DateOpened { get; set; }

        [JsonProperty("dateClosed")]
        public DateTimeOffset? DateClosed { get; set; }

        [IsSearchable]
        [JsonProperty("providerProfileIdType")]
        public string ProviderProfileIdType { get; set; }

        [IsSearchable]
        [JsonProperty("laCode")]
        public string LaCode { get; set; }

        [IsSearchable]
        [JsonProperty("navVendorNo")]
        public string NavVendorNo { get; set; }

        [IsSearchable]
        [JsonProperty("crmAccountId")]
        public string CrmAccountId { get; set; }

        [IsSearchable]
        [JsonProperty("legalName")]
        public string LegalName { get; set; }

        [IsSearchable]
        [JsonProperty("status")]
        public string Status { get; set; }

        [IsSearchable]
        [JsonProperty("phaseOfEducation")]
        public string PhaseOfEducation { get; set; }

        [IsSearchable]
        [JsonProperty("reasonEstablishmentOpened")]
        public string ReasonEstablishmentOpened { get; set; }

        [IsSearchable]
        [JsonProperty("reasonEstablishmentClosed")]
        public string ReasonEstablishmentClosed { get; set; }

        [IsSearchable]
        [JsonProperty("successor")]
        public string Successor { get; set; }

        [IsSearchable]
        [JsonProperty("trustStatus")]
        public string TrustStatus { get; set; }

        [IsSearchable]
        [JsonProperty("trustName")]
        public string TrustName { get; set; }

        [IsSearchable]
        [JsonProperty("trustCode")]
        public string TrustCode { get; set; }

        [IsSearchable]
        [JsonProperty("town")]
        public string Town { get; set; }

        [IsSearchable]
        [JsonProperty("postcode")]
        public string Postcode { get; set; }

        [IsFilterable]
        [JsonProperty("localAuthorityName")]
        public string LocalAuthorityName { get; set; }

        [IsFilterable]
        [JsonProperty("companiesHouseNumber")]
        public string CompaniesHouseNumber { get; set; }

        [IsFilterable]
        [JsonProperty("groupIdNumber")]
        public string GroupIdNumber { get; set; }

        [IsFilterable]
        [JsonProperty("rscRegionName")]
        public string RscRegionName { get; set; }

        [IsFilterable]
        [JsonProperty("rscRegionCode")]
        public string RscRegionCode { get; set; }

        [IsFilterable]
        [JsonProperty("governmentOfficeRegionName")]
        public string GovernmentOfficeRegionName { get; set; }

        [IsFilterable]
        [JsonProperty("governmentOfficeRegionCode")]
        public string GovernmentOfficeRegionCode { get; set; }

        [IsFilterable]
        [JsonProperty("districtName")]
        public string DistrictName { get; set; }

        [IsFilterable]
        [JsonProperty("districtCode")]
        public string DistrictCode { get; set; }

        [IsFilterable]
        [JsonProperty("wardName")]
        public string WardName { get; set; }

        [IsFilterable]
        [JsonProperty("wardCode")]
        public string WardCode { get; set; }

        [IsFilterable]
        [JsonProperty("censusWardName")]
        public string CensusWardName { get; set; }

        [IsFilterable]
        [JsonProperty("censusWardCode")]
        public string CensusWardCode { get; set; }

        [IsFilterable]
        [JsonProperty("middleSuperOutputAreaName")]
        public string MiddleSuperOutputAreaName { get; set; }

        [IsFilterable]
        [JsonProperty("middleSuperOutputAreaCode")]
        public string MiddleSuperOutputAreaCode { get; set; }

        [IsFilterable]
        [JsonProperty("lowerSuperOutputAreaName")]
        public string LowerSuperOutputAreaName { get; set; }

        [IsFilterable]
        [JsonProperty("lowerSuperOutputAreaCode")]
        public string LowerSuperOutputAreaCode { get; set; }

        [IsFilterable]
        [JsonProperty("parliamentaryConstituencyName")]
        public string ParliamentaryConstituencyName { get; set; }

        [IsFilterable]
        [JsonProperty("parliamentaryConstituencyCode")]
        public string ParliamentaryConstituencyCode { get; set; }

        [IsFilterable]
        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        [IsFilterable]
        [JsonProperty("countryName")]
        public string CountryName { get; set; }
    }
}
