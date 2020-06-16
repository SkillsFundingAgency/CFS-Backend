using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.CodeMetadataGenerator.Vb.UnitTests
{
    public class FundingLineCalculationBuilder : TestEntityBuilder
    {
        private string _name;
        private CalculationNamespace? _namespace;
        private string _sourceCodeName;
        private uint _id;

        public FundingLineCalculationBuilder WithSourceCodeName(string sourceCodeName)
        {
            _sourceCodeName = sourceCodeName;

            return this;
        }

        public FundingLineCalculationBuilder WithId(uint id)
        {
            _id = id;

            return this;
        }

        public FundingLineCalculationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public FundingLineCalculationBuilder WithCalculationNamespaceType(CalculationNamespace namespaceType)
        {
            _namespace = namespaceType;

            return this;
        }

        public FundingLineCalculation Build()
        {
            return new FundingLineCalculation
            {
                Id = _id,
                SourceCodeName = _sourceCodeName ?? NewCleanRandomString(),
                Name = _name ?? NewCleanRandomString(),
                Namespace = _namespace.GetValueOrDefault(new RandomEnum<CalculationNamespace>()).ToString(),
            };
        }
    }
}