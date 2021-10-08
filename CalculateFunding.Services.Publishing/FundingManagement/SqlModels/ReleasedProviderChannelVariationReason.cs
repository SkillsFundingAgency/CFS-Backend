using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("ReleasedProviderChannelVariationReasons")]

    public class ReleasedProviderChannelVariationReason
    {
        [Dapper.Contrib.Extensions.Key]
        public int ReleasedProviderChannelVariationReasonId { get; set; }

        [Required]
        public int VariationReasonId { get; set; }

        [Required]
        public int ReleasedProviderVersionChannelId { get; set; }
    }
}
