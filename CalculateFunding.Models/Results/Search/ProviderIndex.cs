using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Models.Results.Search
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "providerindex")]
    public class ProviderIndex
    {
	    [Key]
        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("providerIdType")]
        [IsRetrievable(true)]
        public string ProviderIdType { get; set; }

        [IsSearchable, IsRetrievable(true)]
	    [JsonProperty("ukPrn")]
	    public string UKPRN { get; set; }

		[IsSearchable, IsRetrievable(true)]
		[JsonProperty("urn")]
		public string URN { get; set; }

	    [IsSearchable, IsRetrievable(true)]
	    [JsonProperty("upin")]
	    public string UPIN { get; set; }

        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("dfeEstablishmentNumber")]
        public string DfeEstablishmentNumber { get; set; }

        [IsSearchable, IsRetrievable(true)]
	    [JsonProperty("establishmentNumber")]
	    public string EstablishmentNumber { get; set; }

	    [IsSearchable, IsRetrievable(true)]
	    [JsonProperty("rid")]
	    public string Rid { get; set; }

		[IsSearchable, IsSortable, IsRetrievable(true)]
		[JsonProperty("name")]
		public string Name { get; set; }

        [IsSearchable, IsSortable, IsRetrievable(true)]
        [JsonProperty("legalName")]
        public string LegalName { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("authority")]
		public string Authority { get; set; }

	    [IsFilterable, IsSortable, IsFacetable, IsRetrievable(true)]
	    [JsonProperty("providerType")]
	    public string ProviderType { get; set; }

	    [IsFilterable, IsSortable, IsFacetable, IsRetrievable(true)]
	    [JsonProperty("providerSubType")]
	    public string ProviderSubType { get; set; }

	    [IsFilterable, IsSortable, IsFacetable, IsRetrievable(true)]
	    [JsonProperty("openDate")]
		public DateTimeOffset? OpenDate { get; set; }

	    [IsFilterable, IsSortable, IsFacetable, IsRetrievable(true)]
	    [JsonProperty("closeDate")]
		public DateTimeOffset? CloseDate { get; set; }

        [JsonProperty("laCode")]
        [IsRetrievable(true)]
        public string LACode { get; set; }

        [JsonProperty("crmAccountId")]
        [IsRetrievable(true)]
        public string CrmAccountId { get; set; }

        [JsonProperty("navVendorNo")]
        [IsRetrievable(true)]
        public string NavVendorNo { get; set; }

        [JsonProperty("status")]
        [IsRetrievable(true)]
        public string Status { get; set; }

        [JsonProperty("phaseOfEducation")]
        [IsRetrievable(true)]
        public string PhaseOfEducation { get; set; }

	    [JsonProperty("reasonEstablishmentOpened")]
        [IsRetrievable(true)]
	    public string ReasonEstablishmentOpened { get; set; }

	    [JsonProperty("reasonEstablishmentClosed")]
        [IsRetrievable(true)]
		public string ReasonEstablishmentClosed { get; set; }

		[JsonProperty("successor")]
        [IsRetrievable(true)]
		public string Successor { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("trustStatus")]
        public string TrustStatus { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("trustName")]
        public string TrustName { get; set; }

        [JsonProperty("trustCode")]
        [IsRetrievable(true)]
        public string TrustCode { get; set; }
    }
}