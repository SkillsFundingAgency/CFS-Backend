using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Publishing
{
    public class TemplateMapping : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id => $"templatemapping-{SpecificationId}-{FundingStreamId}";

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("templateMappingItems")]
        public List<TemplateMappingItem> TemplateMappingItems { get; set; }
    }
}
