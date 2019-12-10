using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class CalculationResponseBuilder : TestEntityBuilder
    {
        private string _sourceCode;

        public CalculationResponseBuilder WithSourceCode(string sourceCode)
        {
            _sourceCode = sourceCode;

            return this;
        }
        
        public CalculationResponseModel Build()
        {
            return new CalculationResponseModel
            {
                SourceCode = _sourceCode
            };
        }
    }
}