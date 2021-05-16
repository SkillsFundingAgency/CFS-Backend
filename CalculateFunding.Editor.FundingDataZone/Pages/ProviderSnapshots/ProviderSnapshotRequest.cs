using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Editor.FundingDataZone.Pages.ProviderSnapshots
{
    public class ProviderSnapshotRequest
    {
        [Required, StringLength(128)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int Version { get; set; }

        [Required]
        [Display(Name = "Target date")]
        public DateTime TargetDate { get; set; }

        [Required]
        [Display(Name = "Funding stream")]
        public int? FundingStreamId { get; set; }
    }
}
