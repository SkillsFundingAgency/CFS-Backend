using CalculateFunding.Tests.Common.Helpers;
using FluentValidation.Results;

namespace CalculateFunding.Services.Jobs
{
    public class ValidationFailureBuilder : TestEntityBuilder
    {
        private string _propertyName;
        private string _message;

        public ValidationFailureBuilder WithPropertyName(string propertyName)
        {
            _propertyName = propertyName;

            return this;
        }

        public ValidationFailureBuilder WithErrorMessage(string errorMessage)
        {
            _message = errorMessage;

            return this;
        }
        
        public ValidationFailure Build()
        {
            return new ValidationFailure(
                _propertyName ?? NewRandomString(), 
                _message ?? NewRandomString());
        }
    }
}