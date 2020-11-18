using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Compiler.UnitTests
{
    public class FundingLineBuilder : TestEntityBuilder
    {
        private IEnumerable<FundingLineCalculation> _fundingLineCalculations = Enumerable.Empty<FundingLineCalculation>();
        private string _name;
        private uint? _templateId;
        private string _namespace;
        private string _sourceCodeName;

        public FundingLineBuilder WithTemplateId(uint templateId)
        {
            _templateId = templateId;

            return this;
        }

        public FundingLineBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public FundingLineBuilder WithNamespace(string @namespace)
        {
            _namespace = @namespace;

            return this;
        }

        public FundingLineBuilder WithSourceCodeName(string sourceCodeName)
        {
            _sourceCodeName = sourceCodeName;

            return this;
        }

        public FundingLineBuilder WithCalculations(IEnumerable<FundingLineCalculation> fundingLineCalculations)
        {
            _fundingLineCalculations = fundingLineCalculations;

            return this;
        }

        public FundingLine Build()
        {
            return new FundingLine
            {
                Id = _templateId.GetValueOrDefault((uint)NewRandomNumberBetween(1, int.MaxValue)),
                Name = _name ?? NewRandomString(),
                Namespace = _namespace ?? NewRandomString(),
                SourceCodeName = _sourceCodeName ?? NewRandomString(),
                Calculations = _fundingLineCalculations
            };
        }
    }
}