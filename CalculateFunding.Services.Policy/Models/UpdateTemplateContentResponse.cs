using System;
using CalculateFunding.Services.Core.Extensions;
using FluentValidation.Results;
using ModelStateDictionary = Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary;

namespace CalculateFunding.Services.Policy.Models
{
    public class UpdateTemplateContentResponse
    {
        public bool Succeeded { get; set; }
        
        public string ErrorMessage { get; set; }
        
        public Exception Exception { get; set; }
        
        public ModelStateDictionary ValidationModelState { get; set; }

        public static UpdateTemplateContentResponse Success()
        {
            return new UpdateTemplateContentResponse { Succeeded = true };
        }
        
        public static UpdateTemplateContentResponse ValidationFail(ModelStateDictionary errors)
        {
            return new UpdateTemplateContentResponse
            {
                Succeeded = false,
                ValidationModelState = errors
            };
        }
        
        public static UpdateTemplateContentResponse ValidationFail(string[] errors)
        {
            return new UpdateTemplateContentResponse
            {
                Succeeded = false,
                ValidationModelState = errors.ToModelStateDictionary()
            };
        }

        public static UpdateTemplateContentResponse ValidationFail(ValidationResult errors)
        {
            return new UpdateTemplateContentResponse
            {
                Succeeded = false,
                ValidationModelState = errors.ToModelStateDictionary()
            };
        }

        public static UpdateTemplateContentResponse ValidationFail(string propertyName, string error)
        {
            var validationResult = new ValidationResult();
            validationResult.Errors.Add(new ValidationFailure(propertyName, error));
            return new UpdateTemplateContentResponse
            {
                Succeeded = false,
                ValidationModelState = validationResult.ToModelStateDictionary()
            };
        }

        public static UpdateTemplateContentResponse Fail(string errorMessage)
        {
            return new UpdateTemplateContentResponse
            {
                Succeeded = false,
                ErrorMessage = errorMessage
            };
        }
    }
}