
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Migrations.Fdz.Copy.Models
{
    internal class PaymentOrganisation
    {
        /// <summary>
        /// ID of this organisation within the publishing area
        /// </summary>
        [Required]
        public int PaymentOrganisationId { get; set; }

        /// <summary>
        /// The provider snapshot this organisation is part of
        /// </summary>
        [Required]
        public int ProviderSnapshotId { get; set; }

        /// <summary>
        /// Organisation name
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Type of organisation, eg Local Authority or Academy Trust
        /// </summary>
        [Required]
        public string PaymentOrganisationType { get; set; }

        [Required]
        public string Ukprn { get; set; }

        public string Upin { get; set; }

        public string TrustCode { get; set; }

        public string Urn { get; set; }

        public string LaCode { get; set; }
        public string LaOrg { get; set; }

        public string CompanyHouseNumber { get; set; }
    }
}
