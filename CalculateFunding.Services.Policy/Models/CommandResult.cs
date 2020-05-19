using System;
using CalculateFunding.Services.Core.Extensions;
using FluentValidation.Results;
using ModelStateDictionary = Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary;

namespace CalculateFunding.Services.Policy.Models
{
    public class CommandResult
    {
        public bool Succeeded { get; set; }
        
        public string TemplateId { get; set; }
        
        public string ErrorMessage { get; set; }
        
        public Exception Exception { get; set; }
        
        public ValidationResult ValidationResult { get; set; }
        
        public ModelStateDictionary ValidationModelState { get; set; }

        public static CommandResult Success()
        {
            return new CommandResult
            {
                Succeeded = true
            };
        }
        
        public static CommandResult ValidationFail(string[] errors)
        {
            return new CommandResult
            {
                Succeeded = false,
                ValidationModelState = errors.ToModelStateDictionary()
            };
        }
        
        public static CommandResult ValidationFail(ValidationResult errors)
        {
            return new CommandResult
            {
                Succeeded = false,
                ValidationResult = errors
            };
        }

        public static CommandResult Fail(string errorMessage)
        {
            return new CommandResult
            {
                Succeeded = false,
                ErrorMessage = errorMessage
            };
        }
        
        public static CommandResult ValidationFail(string propertyName, string error)
        {
            var validationResult = new ValidationResult();
            validationResult.Errors.Add(new ValidationFailure(propertyName, error));
            return new CommandResult
            {
                Succeeded = false,
                ValidationResult = validationResult
            };
        }
    }
}