using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Migrations.Fdz.Copy.Models
{
    internal class ProviderSnapshot
    {
        [Required]
        public int ProviderSnapshotId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public int Version { get; set; }

        [Required]
        public DateTime TargetDate { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        public string FundingStreamId { get; set; }
    }
}
