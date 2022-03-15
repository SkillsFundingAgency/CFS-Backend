using Dapper.Contrib.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("FundingGroupProviders")]
    public class FundingGroupProvider
    {
        [ExplicitKey]
        public Guid FundingGroupProviderId { get; set; }

        [Required]
        public Guid FundingGroupVersionId { get; set; }

        [Required]
        public Guid ReleasedProviderVersionChannelId { get; set; }
    }
}
