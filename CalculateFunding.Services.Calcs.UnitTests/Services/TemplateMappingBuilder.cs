using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.Services
{
    public class TemplateMappingBuilder : TestEntityBuilder
    {
        private IEnumerable<TemplateMappingItem> _items;

        public TemplateMappingBuilder WithItems(params TemplateMappingItem[] items)
        {
            _items = items;

            return this;
        }
        
        public TemplateMapping Build()
        {
            return new TemplateMapping
            {
                TemplateMappingItems = _items?.ToList() ?? new List<TemplateMappingItem>()
            };
        }    
    }
}