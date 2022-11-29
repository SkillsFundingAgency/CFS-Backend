using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.FundingDataZone
{
    public class ProviderSnapshot
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
        public string FundingStreamCode { get; set; }

        [Required]
        public string FundingStreamName { get; set; }

        public string FundingPeriodName { get; set; }
    }
}
