using Dapper.Contrib.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("FundingGroupVersionVariationReasons")]
    public class FundingGroupVersionVariationReason
    {
        [ExplicitKey]
        public Guid FundingGroupVersionVariationReasonId { get; set; }

        [Required]
        public int VariationReasonId { get; set; }

        [Required]
        public Guid FundingGroupVersionId { get; set; }
    }
}
