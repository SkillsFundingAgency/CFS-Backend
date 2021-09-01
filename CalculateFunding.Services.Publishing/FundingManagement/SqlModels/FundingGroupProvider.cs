using Dapper.Contrib.Extensions;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("FundingGroupProviders")]
    public class FundingGroupProvider
    {
        [Dapper.Contrib.Extensions.Key]
        public int FundingGroupProviderId { get; set; }

        [Required]
        public int FundingGroupVersionId { get; set; }

        [Required]
        public int ProviderFundingVersionChannelId { get; set; }
    }
}
