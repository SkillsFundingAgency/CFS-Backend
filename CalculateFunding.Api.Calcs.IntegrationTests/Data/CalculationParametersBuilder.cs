using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Calcs.IntegrationTests.SqlExport
{
    public class CalculationParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private string _fundingStreamId;
        private string _specificationId;
        private string _calculationType;
        private string _calculationValueType;
        private string _calculationDataType;
        private string _sourceCodeName;

        public CalculationParametersBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public CalculationParametersBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public CalculationParametersBuilder WithSourceCodeName(string sourceCodeName)
        {
            _sourceCodeName = sourceCodeName;
            return this;
        }

        public CalculationParametersBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;
            return this;
        }

        public CalculationParametersBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;
            return this;
        }

        public CalculationParametersBuilder WithCalculationType(string calculationType)
        {
            _calculationType = calculationType;
            return this;
        }

        public CalculationParametersBuilder WithCalculationValueType(string calculationValueType)
        {
            _calculationValueType = calculationValueType;
            return this;
        }

        public CalculationParametersBuilder WithCalculationDataType(string calculationDataType)
        {
            _calculationDataType = calculationDataType;
            return this;
        }

        public CalculationParameters Build()
        {
            return new CalculationParameters
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                SourceCodeName = _sourceCodeName ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                SpecificationId = _specificationId ?? NewRandomString(),
                CalculationType = _calculationType ?? NewRandomString(),
                CalculationValueType = _calculationValueType ?? NewRandomString(),
                CalculationDataType = _calculationDataType ?? NewRandomString()
            };
        }
    }
}
