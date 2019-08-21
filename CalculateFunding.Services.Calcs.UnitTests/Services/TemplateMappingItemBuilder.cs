using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.Services
{
    public class TemplateMappingItemBuilder : TestEntityBuilder
    {
        private uint? _templateId;
        private string _templateName;
        private string _calculationid;

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

        public TemplateMappingItem Build()
        {
            return new TemplateMappingItem
            {
                Name = _templateName,
                CalculationId = _calculationid,
                TemplateId = _templateId.GetValueOrDefault(NewRandomUint())
            };
        }    
    }
}