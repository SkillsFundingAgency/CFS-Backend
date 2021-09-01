using Dapper.Contrib.Extensions;
using System.ComponentModel.DataAnnotations;
using KeyAttribute = Dapper.Contrib.Extensions.KeyAttribute;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("FundingGroupVersionVariationReasons")]
    public class FundingGroupVersionVariationReason
    {
        [Key]
        public int FundingGroupVersionVariationReasonId { get; set; }

        [Required]
        public int VariationReasonId { get; set; }

        [Required]
        public int FundingGroupVersionId { get; set; }
    }
}
