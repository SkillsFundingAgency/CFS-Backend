using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CalculateFunding.Models.Results.Search
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "providerversionsindex")]
    public class ProviderVersionsIndex
    {
        [Key]
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{ProviderVersionId}_{UKPRN}";
            }
        }

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
    }
}
