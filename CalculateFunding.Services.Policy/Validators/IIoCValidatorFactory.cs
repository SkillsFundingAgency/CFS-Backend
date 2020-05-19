using System;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;

namespace CalculateFunding.Services.Policy.Validators
{
    public interface IIoCValidatorFactory
    {
        IValidator CreateInstance(Type validatorType);
        IValidator<T> CreateInstance<T>();
        Task ValidateAndThrow<T>(T model);
        Task<ValidationResult> Validate<T>(T model);
    }
}