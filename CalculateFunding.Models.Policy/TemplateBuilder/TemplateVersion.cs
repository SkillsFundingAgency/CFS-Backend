using System;
using System.Collections.Generic;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Policy.TemplateBuilder
{
    /// <summary>
    /// A version of a template. Used to track changes while a template is built.
    /// </summary>
    public class TemplateVersion : VersionedItem
    {
        /// <summary>
        /// Cosmos ID for the document. This will be used as the document ID when saving to cosmos
        /// </summary>
        [JsonProperty("id")]
        public override string Id => $"templateVersion-{TemplateId}-{Version}";

        [JsonProperty("templateId")]
        public string TemplateId { get; set; }
        
        /// <summary>
        /// contains template itself in its full JSON glory (theoretically any schema supported)
        /// </summary>
        [JsonProperty("TemplateJson")]
        public string TemplateJson { get; set; }
        
        /// <summary>
        /// Funding Stream ID. eg PSG, DSG
        /// </summary>
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("schemaVersion")]
        public string SchemaVersion { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Entity ID for cosmos versioning. This refers to the parent cosmos ID
        /// </summary>
        [JsonProperty("entityId")]
        public override string EntityId => $"template-{SchemaVersion}-{FundingStreamId}";

        /// <summary>
        /// Status of Template Build
        /// </summary>
        [JsonProperty("status")]
        public TemplateStatus Status { get; set; }

        /// <summary>
        /// IDs of predecessor template builds
        /// </summary>
        [JsonProperty("predecessors")]
        public ICollection<string> Predecessors { get; set; }
        
        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<TemplateVersion>(json);
        }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                FundingStreamId, 
                SchemaVersion, 
                Version, 
                Status, 
                Name, 
                Description);
        }
    }
}