using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using FluentValidation;
using FluentValidation.Results;
using Serilog;

namespace CalculateFunding.Services.Policy.Validators
{
    public class ValidatorFactory : ValidatorFactoryBase, IIoCValidatorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _log;

        public ValidatorFactory(IServiceProvider serviceProvider, ILogger log)
        {
            _serviceProvider = serviceProvider;
            _log = log;
        }
        
        public override IValidator CreateInstance(Type validatorType)
        {
            try
            {
                return _serviceProvider.GetService(validatorType) as IValidator;
            }
            catch (Exception e)
            {
                _log.Error(e, $"Failed to load validator for {nameof(validatorType)}");
                Console.WriteLine(e);
                throw;
            }
        }
        
        public IValidator<T> CreateInstance<T>()
        {
            try
            {
                return _serviceProvider.GetService(typeof(AbstractValidator<T>)) as IValidator<T>;
            }
            catch (Exception e)
            {
                _log.Error(e, $"Failed to load validator for {typeof(T).FullName}");
                throw;
            }
        }
        
        public async Task ValidateAndThrow<T>(T model)
        {
            Guard.ArgumentNotNull(model, typeof(T).Name);
            var validator = CreateInstance<T>();
            if (validator == null)
                throw new ArgumentNullException("Please register missing validator for type " + typeof(T).Name);
            await validator.ValidateAndThrowAsync(model);
        }

        public async Task<ValidationResult> Validate<T>(T model)
        {
            Guard.ArgumentNotNull(model, typeof(T).Name);
            var validator = CreateInstance<T>();
            if (validator == null)
                throw new ArgumentNullException("Please register missing validator for type " + typeof(T).Name);
            return await validator.ValidateAsync(model);
        }
    }

    public static class ValidatorExtensions
    {
        public static ValidationResult And(this ValidationResult result1, ValidationResult result2)
        {
            result1.Errors.AddRange(result2.Errors);
            return result1;
        }
    }
}