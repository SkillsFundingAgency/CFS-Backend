using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Specs
{
    public class ProviderSnapshotDataLoadRequest
    {
        [Required]
        public string SpecificationId { get; set; }

        [Required]
        public int ProviderSnapshotId { get; set; }

        [Required]
        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }
    }
}
