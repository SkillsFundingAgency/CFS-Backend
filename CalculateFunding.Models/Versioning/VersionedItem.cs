using System;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Versioning
{

    public abstract class VersionedItem : IIdentifiable
    {
        [JsonProperty("id")]
        public abstract string Id
        {
            get;
        }

        [JsonProperty("entityId")]
        public abstract string EntityId
        {
            get;
        }

        [JsonProperty("version")]

        public int Version { get; set; }

        [JsonProperty("date")]

        public DateTimeOffset Date { get; set; }

        [JsonProperty("author")]

        public Reference Author { get; set; }

        [JsonProperty("comment")]

        public string Comment { get; set; }

        [JsonProperty("publishStatus")]
        public PublishStatus PublishStatus { get; set; }


        public abstract VersionedItem Clone();
    }
}