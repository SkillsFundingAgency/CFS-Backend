using System;
using FluentValidation.Results;

namespace CalculateFunding.Services.Policy.Models
{
    public class UpdateTemplateMetadataResponse
    {
        public bool Succeeded { get; set; }
        
        public string TemplateId { get; set; }
        
        public string ErrorMessage { get; set; }
        
        public Exception Exception { get; set; }
        
        public ValidationResult ValidationResult { get; set; }

        public static UpdateTemplateMetadataResponse Success()
        {
            return new UpdateTemplateMetadataResponse
            {
                Succeeded = true
            };
        }
        
        public static UpdateTemplateMetadataResponse ValidationFail(ValidationResult errors)
        {
            return new UpdateTemplateMetadataResponse
            {
                Succeeded = false,
                ValidationResult = errors
            };
        }

        public static UpdateTemplateMetadataResponse Fail(string errorMessage)
        {
            return new UpdateTemplateMetadataResponse
            {
                Succeeded = false,
                ErrorMessage = errorMessage
            };
        }
        
        public static UpdateTemplateMetadataResponse ValidationFail(string propertyName, string error)
        {
            var validationResult = new ValidationResult();
            validationResult.Errors.Add(new ValidationFailure(propertyName, error));
            return new UpdateTemplateMetadataResponse
            {
                Succeeded = false,
                ValidationResult = validationResult
            };
        }
    }
}