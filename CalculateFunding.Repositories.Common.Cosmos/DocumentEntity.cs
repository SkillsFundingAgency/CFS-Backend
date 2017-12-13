using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using CalculateFunding.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Repositories.Common.Cosmos
{
    [DataContract]
    public class DocumentEntity<T> where T : Reference
    {
        public DocumentEntity(T content = null)
        {
            DocumentType = typeof(T).Name;
            Content = content;
        }

        [JsonProperty("id")]
        [Key]
        public string Id => Content?.Id;

        [JsonProperty("documentType")]
        public string DocumentType { get; set; }

        [JsonProperty("content")]
        public T Content { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }
    }
}