using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.CodeMetadataGenerator.Vb.UnitTests
{
    public class CalculationBuilder : TestEntityBuilder
    {
        private string _name;
        private CalculationNamespace? _namespace;
        private string _source;
        private string _sourceCodeName;
        private string _id;
        private Reference _fundingStream;

        public CalculationBuilder WithSourceCodeName(string sourceCodeName)
        {
            _sourceCodeName = sourceCodeName;

            return this;
        }

        public CalculationBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public CalculationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public CalculationBuilder WithFundingStream(Reference fundingStream)
        {
            _fundingStream = fundingStream;

            return this;
        }

        public CalculationBuilder WithCalculationNamespaceType(CalculationNamespace namespaceType)
        {
            _namespace = namespaceType;

            return this;
        }

        public CalculationBuilder WithSourceCode(string sourceCode)
        {
            _source = sourceCode;

            return this;
        }

        public Calculation Build()
        {
            return new Calculation
            {
                Id = _id ?? NewCleanRandomString(),
                Current = new CalculationVersion
                {
                    SourceCode = _source,
                    SourceCodeName = _sourceCodeName ?? NewCleanRandomString(),
                    Name = _name ?? NewCleanRandomString(),
                    Namespace = _namespace.GetValueOrDefault(
                        new RandomEnum<CalculationNamespace>()
                    )
                },
                FundingStreamId = _fundingStream?.Id ?? NewCleanRandomString(),
            };
        }
    }
}