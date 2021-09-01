using Dapper.Contrib.Extensions;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("Channels")]
    public class Channel
    {
        /// <summary>
        /// SQL primary key
        /// </summary>
        [Dapper.Contrib.Extensions.Key]
        public int ChannelId { get; set; }

        /// <summary>
        /// Channel code (key) for configuration matching in CFS, eg Contracting or Statement
        /// </summary>
        [Required, StringLength(32)]
        public string ChannelCode { get; set; }

        /// <summary>
        /// Channel display name
        /// </summary>
        [Required, StringLength(64)]
        public string ChannelName { get; set; }

        /// <summary>
        /// URL key for the external API to expose this channel on the CFS external API
        /// </summary>
        [Required, StringLength(32)]
        public string UrlKey { get; set; }
    }
}
