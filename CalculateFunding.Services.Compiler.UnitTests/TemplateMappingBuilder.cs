using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Compiler.UnitTests
{
    public class TemplateMappingBuilder : TestEntityBuilder
    {
        private IEnumerable<TemplateMappingItem> _items;
        private string _specificationId;

        public TemplateMappingBuilder WithItems(params TemplateMappingItem[] items)
        {
            _items = items;

            return this;
        }

        public TemplateMappingBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public TemplateMapping Build()
        {
            return new TemplateMapping
            {
                TemplateMappingItems = _items?.ToList() ?? new List<TemplateMappingItem>(),
                SpecificationId = _specificationId ?? NewRandomString()
            };
        }    
    }
}