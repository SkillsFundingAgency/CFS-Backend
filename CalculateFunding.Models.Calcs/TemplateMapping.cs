using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
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
