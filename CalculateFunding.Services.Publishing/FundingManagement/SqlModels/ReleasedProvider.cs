using Dapper.Contrib.Extensions;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("ReleasedProviders")]
    public class ReleasedProvider
    {
        [Dapper.Contrib.Extensions.Key]
        public int ReleasedProviderId { get; set; }

        [Required, StringLength(64)]
        public string SpecificationId { get; set; }

        [Required, StringLength(32)]
        public string ProviderId { get; set; }
    }
}
