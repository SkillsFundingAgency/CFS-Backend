using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Allocations.Models.Framework
{
    [DataContract]
    public abstract class DocumentEntity
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("documentType")]
        public string DocumentType { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }
    }
}