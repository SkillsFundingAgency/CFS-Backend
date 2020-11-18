using System;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Publishing.Models
{
    public class LatestPublishedDate
    {
        [JsonProperty("value")]
        public DateTime? Value { get; set; }
    }
}