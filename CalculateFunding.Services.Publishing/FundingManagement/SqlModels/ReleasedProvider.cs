using Dapper.Contrib.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("ReleasedProviders")]
    public class ReleasedProvider
    {
        [ExplicitKey]
        public Guid ReleasedProviderId { get; set; }

        [Required, StringLength(64)]
        public string SpecificationId { get; set; }

        [Required, StringLength(32)]
        public string ProviderId { get; set; }
    }
}
