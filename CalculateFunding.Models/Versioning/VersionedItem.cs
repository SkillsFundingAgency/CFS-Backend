using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Versioning
{

    public abstract class VersionedItem
    {
        [JsonProperty("version")]

        public int Version { get; set; }

        [JsonProperty("date")]

        public DateTime Date { get; set; }

        [JsonProperty("author")]

        public Reference Author { get; set; }

        [JsonProperty("comment")]

        public string Commment { get; set; }

        [JsonProperty("publishStatus")]
        public PublishStatus PublishStatus { get; set; }


        public abstract VersionedItem Clone();
    }
}