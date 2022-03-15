using Dapper.Contrib.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("ReleasedProviderVersions")]
    public class ReleasedProviderVersion
    {
        [ExplicitKey]
        public Guid ReleasedProviderVersionId { get; set; }

        [Required]
        public Guid ReleasedProviderId { get; set; }

        [Required]
        public int MajorVersion { get; set; }

        [Required]
        public int MinorVersion { get; set; }

        /// <summary>
        /// Funding ID according to the Funding Data Model, eg 1619-AS-2122-10000045-3_0
        /// </summary>
        [Required]
        public string FundingId { get; set; }

        [Required]
        public decimal TotalFunding { get; set; }

        /// <summary>
        /// Providers service - core provider version ID
        /// </summary>
        [Required]
        public string CoreProviderVersionId { get; set; }
    }
}
