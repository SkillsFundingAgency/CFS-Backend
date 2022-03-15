using Dapper.Contrib.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("ReleasedProviderVersionChannels")]
    public class ReleasedProviderVersionChannel
    {
        [ExplicitKey]
        public Guid ReleasedProviderVersionChannelId { get; set; }

        [Required]
        public Guid ReleasedProviderVersionId { get; set; }

        [Required]
        public int ChannelId { get; set; }

        [Required]
        public DateTime StatusChangedDate { get; set; }

        [Required]
        public string AuthorId { get; set; }

        [Required]
        public string AuthorName { get; set; }
    }
}
