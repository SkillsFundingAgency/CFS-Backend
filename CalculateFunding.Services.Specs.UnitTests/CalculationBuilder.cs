using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class CalculationBuilder : TestEntityBuilder
    {
        private IEnumerable<ReferenceData> _referenceData = Enumerable.Empty<ReferenceData>();
        private uint _templateCalculationId = 0;

        public CalculationBuilder WithReferenceData(params ReferenceData[] referenceData)
        {
            _referenceData = referenceData;

            return this;
        }

        public CalculationBuilder WithTemplateCalculationId(uint templateCalculationId)
        {
            _templateCalculationId = templateCalculationId;

            return this;
        }

        public Calculation Build()
        {
            return new Calculation
            {
                ReferenceData = _referenceData.ToArray(),
                TemplateCalculationId = _templateCalculationId
            };
        }
    }
}