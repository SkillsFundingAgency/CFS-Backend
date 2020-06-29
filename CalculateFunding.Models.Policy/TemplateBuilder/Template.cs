using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Policy.TemplateBuilder
{
    /// <summary>
    /// A version of a template. Used to track changes while a template is built.
    /// </summary>
    public class Template : Reference, IIdentifiable
    {
        /// <summary>
        /// Cosmos document id
        /// </summary>
        [JsonProperty("id")]
        public new string Id => base.Id = $"template-{TemplateId}";

        [JsonProperty("templateId")]
        public string TemplateId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Funding Stream ID. eg PSG, DSG
        /// </summary>
        [JsonProperty("fundingStream")]
        public FundingStream FundingStream { get; set; }

        /// <summary>
        /// Funding Period ID
        /// </summary>
        [JsonProperty("fundingPeriod")]
        public FundingPeriod FundingPeriod { get; set; }
        
        /// <summary>
        /// Current version of the provider
        /// </summary>
        [JsonProperty("current")]
        public TemplateVersion Current { get; set; }

        [JsonProperty("released")]
        public TemplateVersion Released { get; set; }

        public bool HasPredecessor(string templateId)
        {
            return Current?.Predecessors?.Count(_ => _?.ToLower().Trim() == templateId?.ToLower().Trim()) >= 1;
        }

        public void AddPredecessor(string templateId)
        {
            Current.Predecessors ??= new List<string>();
            Current.Predecessors.Add(templateId);
        }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Current, Released);
        }
    }
}