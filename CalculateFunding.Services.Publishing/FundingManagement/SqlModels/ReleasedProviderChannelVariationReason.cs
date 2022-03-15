using Dapper.Contrib.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using TableAttribute = Dapper.Contrib.Extensions.TableAttribute;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("ReleasedProviderChannelVariationReasons")]

    public class ReleasedProviderChannelVariationReason
    {
        [ExplicitKey]
        public Guid ReleasedProviderChannelVariationReasonId { get; set; }

        [Required]
        public int VariationReasonId { get; set; }

        [Required]
        public Guid ReleasedProviderVersionChannelId { get; set; }
    }
}
