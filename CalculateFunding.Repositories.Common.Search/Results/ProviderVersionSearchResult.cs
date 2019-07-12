using System;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class ProviderVersionSearchResult
    {
        public string Id { get; set; }

        public string ProviderVersionId { get; set; }

        public string ProviderId { get; set; }

        public string Name { get; set; }

        public string URN { get; set; }

        public string UKPRN { get; set; }

        public string UPIN { get; set; }

        public string EstablishmentNumber { get; set; }

        public string DfeEstablishmentNumber { get; set; }

        public string Authority { get; set; }

        public string ProviderType { get; set; }

        public string ProviderSubType { get; set; }

        public DateTimeOffset? DateOpened { get; set; }

        public DateTimeOffset? DateClosed { get; set; }

        public string ProviderProfileIdType { get; set; }

        public string LaCode { get; set; }

        public string NavVendorNo { get; set; }

        public string CrmAccountId { get; set; }

        public string LegalName { get; set; }

        public string Status { get; set; }

        public string PhaseOfEducation { get; set; }

        public string ReasonEstablishmentOpened { get; set; }

        public string ReasonEstablishmentClosed { get; set; }

        public string Successor { get; set; }

        public string TrustStatus { get; set; }

        public string TrustName { get; set; }

        public string TrustCode { get; set; }

        public string Town { get; set; }

        public string Postcode { get; set; }
    }
}
