using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class TemplateMappingItemBuilder : TestEntityBuilder
    {
        private uint? _templateId;
        private string _templateName;
        private string _calculationid;
        private TemplateMappingEntityType _entityType;

        public TemplateMappingItemBuilder WithCalculationId(string calculationId)
        {
            _calculationid = calculationId;

            return this;
        }
        
        public TemplateMappingItemBuilder WithTemplateId(uint templateId)
        {
            _templateId = templateId;

            return this;
        }

        public TemplateMappingItemBuilder WithName(string templateName)
        {
            _templateName = templateName;

            return this;
        }

        public TemplateMappingItemBuilder WithEntityType(TemplateMappingEntityType entityType)
        {
            _entityType = entityType;

            return this;
        }

        public TemplateMappingItem Build()
        {
            return new TemplateMappingItem
            {
                Name = _templateName,
                CalculationId = _calculationid,
                TemplateId = _templateId.GetValueOrDefault(NewRandomUint()),
                EntityType = _entityType
            };
        }    
    }
}