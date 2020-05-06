using System;
using CalculateFunding.Common.Models.Versioning;
using CalculateFunding.Models.Policy.TemplateBuilder;
using PublishStatus = CalculateFunding.Models.Versioning.PublishStatus;

namespace CalculateFunding.Services.Policy.Models
{
    public class TemplateResponse
    {
        public string TemplateId { get; set; }
        
        /// <summary>
        /// contains template itself in its full JSON glory (theoretically any schema supported)
        /// </summary>
        public string TemplateJson { get; set; }
        
        /// <summary>
        /// Funding Stream ID. eg PSG, DSG
        /// </summary>
        public string FundingStreamId { get; set; }

        public string SchemaVersion { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
        
        /// <summary>
        /// Status of Template Build
        /// </summary>
        public TemplateStatus Status { get; set; }
        
        public PublishStatus PublishStatus { get; set; }
        
        public string Comments { get; set; }
        
        public int Version { get; set; }
        
        public int MinorVersion { get; set; }
        
        public int MajorVersion { get; set; }
        
        public string AuthorId { get; set; }
        
        public string AuthorName { get; set; }
        
        public DateTime LastModificationDate { get; set; }
    }
}