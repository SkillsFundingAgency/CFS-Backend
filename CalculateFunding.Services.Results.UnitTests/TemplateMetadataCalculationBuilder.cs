using CalculateFunding.Tests.Common.Helpers;
using TemplateMetadataCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class TemplateMetadataCalculationBuilder : TestEntityBuilder
    {
        private string _name;
        private uint _templateCalculationId;

        public TemplateMetadataCalculationBuilder WithTemplateCalculationId(uint templateCalculationId)
        {
            _templateCalculationId = templateCalculationId;

            return this;
        }

        public TemplateMetadataCalculationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public TemplateMetadataCalculation Build()
            => new TemplateMetadataCalculation
            {
                TemplateCalculationId = _templateCalculationId,
                Name = _name ?? NewCleanRandomString(),
            };
    }
}
