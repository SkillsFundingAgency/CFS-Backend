using Dapper.Contrib.Extensions;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("ReleasedProviderVersionChannels")]
    public class ReleasedProviderVersionChannel
    {
        [Dapper.Contrib.Extensions.Key]
        public int ReleasedProviderVersionChannelId { get; set; }

        [Required]
        public int ReleasedProviderVersionId { get; set; }

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
