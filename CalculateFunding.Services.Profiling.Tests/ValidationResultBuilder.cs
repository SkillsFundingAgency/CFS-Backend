using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.Extensions;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using FluentValidation.Results;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class ValidationResultBuilder : TestEntityBuilder
    {
        private IEnumerable<ValidationFailure> _failures;

        public ValidationResultBuilder WithFailures(params ValidationFailure[] failures)
        {
            _failures = failures;

            return this;
        }
        
        public ValidationResult Build()
        {
            ValidationResult validationResult = new ValidationResult();
            
            validationResult.Errors.AddRange(_failures ?? Enumerable.Empty<ValidationFailure>());
            
            return validationResult;
        }
    }
}