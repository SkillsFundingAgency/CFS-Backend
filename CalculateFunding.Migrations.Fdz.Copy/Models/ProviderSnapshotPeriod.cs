using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Migrations.Fdz.Copy.Models
{
    internal class ProviderSnapshotPeriod
    {
        [Required]
        public int ProviderSnapshotId { get; set; }

        [Required]
        public string FundingPeriodName { get; set; }
    }
}
