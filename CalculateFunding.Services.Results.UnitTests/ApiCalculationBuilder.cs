using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class ApiCalculationBuilder : TestEntityBuilder
    {
        private string _name;
        private CalculationNamespace? _namespace;
        private string _source;
        private string _sourceCodeName;
        private string _id;
        private Reference _fundingStream;
        private CalculationValueType _calculationValueType;
        private CalculationDataType _calculationDataType;
        private CalculationType _calculationType;

        public ApiCalculationBuilder WithSourceCodeName(string sourceCodeName)
        {
            _sourceCodeName = sourceCodeName;

            return this;
        }

        public ApiCalculationBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public ApiCalculationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public ApiCalculationBuilder WithFundingStream(Reference fundingStream)
        {
            _fundingStream = fundingStream;

            return this;
        }

        public ApiCalculationBuilder WithCalculationNamespaceType(CalculationNamespace namespaceType)
        {
            _namespace = namespaceType;

            return this;
        }

        public ApiCalculationBuilder WithSourceCode(string sourceCode)
        {
            _source = sourceCode;

            return this;
        }

        public ApiCalculationBuilder WithValueType(CalculationValueType calculationValueType)
        {
            _calculationValueType = calculationValueType;
            return this;
        }

        public ApiCalculationBuilder WithDataType(CalculationDataType calculationDataType)
        {
            _calculationDataType = calculationDataType;
            return this;
        }

        public ApiCalculationBuilder WithType(CalculationType calculationType)
        {
            _calculationType = calculationType;
            return this;
        }

        public Calculation Build()
        {
            return new Calculation
            {
                Id = _id ?? NewCleanRandomString(),
                SourceCode = _source,
                SourceCodeName = _sourceCodeName ?? NewCleanRandomString(),
                Name = _name ?? NewCleanRandomString(),
                Namespace = _namespace.GetValueOrDefault(
                    new RandomEnum<CalculationNamespace>()
                ),
                ValueType = _calculationValueType,
                DataType = _calculationDataType,
                CalculationType = _calculationType,
                FundingStreamId = _fundingStream?.Id ?? NewCleanRandomString(),
            };
        }
    }
}
