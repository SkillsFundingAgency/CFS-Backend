using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

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
        private CalculationValueType _calculationValueType;
        private CalculationDataType _calculationDataType;
        private IEnumerable<string> _allowedEnumTypeValues;

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

        public CalculationBuilder WithValueType(CalculationValueType  calculationValueType)
        {
            _calculationValueType = calculationValueType;
            return this;
        }

        public CalculationBuilder WithDataType(CalculationDataType calculationDataType)
        {
            _calculationDataType = calculationDataType;
            return this;
        }

        public CalculationBuilder WithAllowedEnumTypeValues(IEnumerable<string> allowedEnumTypeValues)
        {
            _allowedEnumTypeValues = allowedEnumTypeValues;
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
                    ),
                    ValueType = _calculationValueType,
                    DataType = _calculationDataType,
                    AllowedEnumTypeValues = _allowedEnumTypeValues
                },
                FundingStreamId = _fundingStream?.Id ?? NewCleanRandomString(),
            };
        }
    }
}