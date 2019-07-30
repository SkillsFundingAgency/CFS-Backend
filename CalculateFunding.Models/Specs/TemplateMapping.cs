using CalculateFunding.Common.Models;
using System.Collections.Generic;

namespace CalculateFunding.Models.Specs
{
    public class TemplateMapping : IIdentifiable
    {
        public string Id => $"templatemapping-{SpecificationId}";
        public string SpecificationId { get; set; }
        public List<TemplateMappingItem> TemplateMappingItems { get; set; }
    }
}
