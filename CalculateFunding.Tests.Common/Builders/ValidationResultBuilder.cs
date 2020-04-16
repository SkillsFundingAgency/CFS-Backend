using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentValidation.Results;

namespace CalculateFunding.Tests.Common.Builders
{
    public class ValidationResultBuilder : TestEntityBuilder
    {
        private IEnumerable<ValidationFailure> _failures = Enumerable.Empty<ValidationFailure>();

        public ValidationResultBuilder WithValidationFailures(params ValidationFailure[] failures)
        {
            _failures = failures;

            return this;
        }
        
        public ValidationResult Build()
        {
            ValidationResult validationResult = new ValidationResult();

            validationResult.Errors.AddRange(_failures);
            
            return validationResult;
        }
    }
}