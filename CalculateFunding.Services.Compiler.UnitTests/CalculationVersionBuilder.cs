using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Compiler.UnitTests
{
    public class CalculationVersionBuilder : TestEntityBuilder
    {
        private string _name;
        private CalculationValueType _calculationValueType;
        private string _sourceCodeName;
        private string _sourceCode;

        public CalculationVersionBuilder WithSourceCodeName(string sourceCodeName)
        {
            _sourceCodeName = sourceCodeName;

            return this;
        }

        public CalculationVersionBuilder WithSourceCode(string sourceCode)
        {
            _sourceCode = sourceCode;

            return this;
        }
        
        public CalculationVersionBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public CalculationVersionBuilder WithValueType(CalculationValueType calculationValueType)
        {
            _calculationValueType = calculationValueType;

            return this;
        }

        public CalculationVersion Build()
        {
            return new CalculationVersion
            {
                SourceCode = _sourceCode ?? "return 0",
                SourceCodeName = _sourceCodeName ?? NewRandomString().Replace("-", ""),
                Name = _name,
                ValueType = _calculationValueType
            };
        }
    }
}