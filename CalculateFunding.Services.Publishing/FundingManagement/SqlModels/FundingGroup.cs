using System;
using System.ComponentModel.DataAnnotations;
using Dapper.Contrib.Extensions;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("FundingGroups")]
    public class FundingGroup
    {
        [Dapper.Contrib.Extensions.Key]
        public int FundingGroupId { get; set; }

        [Required, StringLength(64)]
        public string SpecificationId { get; set; }

        [Required]
        public int ChannelId { get; set; }

        [Required]
        public int GroupingReasonId { get; set; }

        [Required, StringLength(128)]
        public string OrganisationGroupTypeCode { get; set; }

        [Required, StringLength(128)]
        public string OrganisationGroupTypeIdentifier { get; set; }

        [Required, StringLength(128)]
        public string OrganisationGroupIdentifierValue { get; set; }

        [Required, StringLength(256)]
        public string OrganisationGroupName { get; set; }

        [Required, StringLength(256)]
        public string OrganisationGroupSearchableName { get; set; }

        [Required, StringLength(256)]
        public string OrganisationGroupTypeClassification { get; set; }
    }
}
