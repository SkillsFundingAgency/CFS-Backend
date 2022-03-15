using Dapper.Contrib.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("FundingGroupVersions")]
    public class FundingGroupVersion
    {
        [ExplicitKey]
        public Guid FundingGroupVersionId { get; set; }

        [Required]
        public Guid FundingGroupId { get; set; }

        [Required]
        public int ChannelId { get; set; }

        [Required]
        public int GroupingReasonId { get; set; }

        [Required]
        public DateTime StatusChangedDate { get; set; }

        [Required]
        public int MajorVersion { get; set; }

        [Required]
        public int MinorVersion { get; set; }

        [Required, StringLength(64)]
        public string TemplateVersion { get; set; }

        [Required, StringLength(64)]
        public string SchemaVersion { get; set; }

        [Required, StringLength(64)]
        public string JobId { get; set; }

        [Required, StringLength(128)]
        public string CorrelationId { get; set; }

        [Required]
        public int FundingStreamId { get; set; }

        [Required]
        public int FundingPeriodId { get; set; }

        [Required, StringLength(64)]
        public string FundingId { get; set; }

        [Required]
        public decimal TotalFunding { get; set; }

        [Required]
        public DateTime ExternalPublicationDate { get; set; }

        [Required]
        public DateTime EarliestPaymentAvailableDate { get; set; }
    }
}
