using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.Models
{
    public class ChannelRequest
    {
        [JsonProperty("channelCode")]
        [Required, StringLength(32)]
        public string ChannelCode { get; set; }

        [JsonProperty("channelName")]
        [Required, StringLength(64)]
        public string ChannelName { get; set; }

        [JsonProperty("urlKey")]
        [Required, StringLength(32)]
        public string UrlKey { get; set; }
    }
}
