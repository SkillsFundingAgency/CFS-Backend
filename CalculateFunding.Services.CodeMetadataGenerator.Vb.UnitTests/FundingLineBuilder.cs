using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.CodeMetadataGenerator.Vb.UnitTests
{
    public class FundingLineBuilder : TestEntityBuilder
    {
        private uint _id;
        private IEnumerable<FundingLineCalculation> _calculations;
        private IEnumerable<FundingLine> _fundingLines;
        private string _name;
        private string _namespace;
        private string _sourceCodeName;

        public FundingLineBuilder WithId(uint id)
        {
            _id = id;

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

        public FundingLineBuilder WithCalculations(params FundingLineCalculation[] calculations)
        {
            _calculations = calculations;

            return this;
        }
        public FundingLineBuilder WithFundingLines(params FundingLine[] fundingLines)
        {
            _fundingLines = fundingLines;

            return this;
        }

        public FundingLine Build()
        {
            return new FundingLine
            {
                Id = _id,
                Calculations = _calculations ?? Enumerable.Empty<FundingLineCalculation>(),
                FundingLines = _fundingLines ?? Enumerable.Empty<FundingLine>(),
                Name = _name ?? NewCleanRandomString(),
                Namespace = _namespace ?? NewCleanRandomString(),
                SourceCodeName = _sourceCodeName ?? NewCleanRandomString()
            };
        }
    }
}