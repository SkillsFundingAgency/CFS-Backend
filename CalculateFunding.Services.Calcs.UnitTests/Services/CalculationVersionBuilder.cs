using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.Services
{
    public class CalculationVersionBuilder : TestEntityBuilder
    {
        private string _calculationId;
        private string _name;
        private string _namespace;
        private string _sourceCode;
        private CalculationValueType _calculationValueType;
        private PublishStatus _publishStatus;

        public CalculationVersionBuilder WithNameSpace(string nameSpace)
        {
            _namespace = nameSpace;

            return this;
        }

        public CalculationVersionBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public CalculationVersionBuilder WithCalculationId(string calculationId)
        {
            _calculationId = calculationId;

            return this;
        }

        public CalculationVersionBuilder WithSourceCode(string sourceCode)
        {
            _sourceCode = sourceCode;

            return this;
        }

        public CalculationVersionBuilder WithValueType(CalculationValueType calculationValueType)
        {
            _calculationValueType = calculationValueType;

            return this;
        }

        public CalculationVersionBuilder WithPublishStatus(PublishStatus publishStatus)
        {
            _publishStatus = publishStatus;

            return this;
        }

        public CalculationVersion Build()
        {
            string name = _name ?? new RandomString();

            return new CalculationVersion
            {
                CalculationId = _calculationId ?? new RandomString(),
                Name = name,
                Namespace = CalculationNamespace.Additional,
                SourceCode = _sourceCode,
                SourceCodeName = new VisualBasicTypeIdentifierGenerator().GenerateIdentifier(name),
                ValueType = _calculationValueType,
                PublishStatus = _publishStatus
            };
        }
    }
}
