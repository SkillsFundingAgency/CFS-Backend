using System;
using Newtonsoft.Json;

namespace CalculateFunding.Api.Providers.ViewModels
{
    public class MasterProviderViewModel
    {
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
        public TrustStatusViewModel TrustStatus { get; set; }

        [JsonProperty("trustName")]
        public string TrustName { get; set; }

        [JsonProperty("trustCode")]
        public string TrustCode { get; set; }
    }
}
