using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Allocations.Repository
{
    [DataContract]
    public abstract class DocumentEntity
    {
        [JsonProperty("id")]
        public abstract string Id { get; }

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