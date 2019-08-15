using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.Services
{
    public class TemplateMappingItemBuilder : TestEntityBuilder
    {
        private uint? _templateId;
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
        
        public TemplateMappingItem Build()
        {
            return new TemplateMappingItem
            {
                CalculationId = _calculationid,
                TemplateId = _templateId.GetValueOrDefault(NewRandomUint())
            };
        }    
    }
}