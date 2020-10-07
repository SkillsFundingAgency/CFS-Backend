using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using FluentValidation.Results;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class ValidationFailureBuilder : TestEntityBuilder
    {
        private string _propertyName;
        private string _errorMessage;

        public ValidationFailureBuilder WithPropertyName(string propertyName)
        {
            _propertyName = propertyName;

            return this;
        }

        public ValidationFailureBuilder WithErrorMessage(string errorMessage)
        {
            _errorMessage = errorMessage;

            return this;
        }
        
        public ValidationFailure Build()
        {
            return new ValidationFailure(_propertyName ?? NewRandomString(), _errorMessage ?? NewRandomString());
        }
    }
}