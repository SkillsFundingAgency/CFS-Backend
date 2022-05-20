using Dapper.Contrib.Extensions;

namespace CalculateFunding.Services.FundingDataZone.SqlModels
{
    [Table("PaymentOrganisation")]
    public class PublishingAreaOrganisation
    {
        [Key]
        public int PaymentOrganisationId { get; set; }

        public int ProviderSnapshotId { get; set; }

        public string Name { get; set; }

        public string PaymentOrganisationType { get; set; }

        public string Ukprn { get; set; }

        public string Upin { get; set; }

        public string TrustCode { get; set; }

        public string Urn { get; set; }

        public string LaCode { get; set; }
        public string LaOrg { get; set; }

        public string CompanyHouseNumber { get; set; }
    }
}
