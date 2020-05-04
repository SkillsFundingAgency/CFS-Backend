using System;
using FluentValidation;
using FluentValidation.Results;

namespace CalculateFunding.Services.Policy.Validators
{
    public interface IIoCValidatorFactory
    {
        IValidator CreateInstance(Type validatorType);
        IValidator<T> CreateInstance<T>();
        void ValidateAndThrow<T>(T model);
        ValidationResult Validate<T>(T model);
    }
}