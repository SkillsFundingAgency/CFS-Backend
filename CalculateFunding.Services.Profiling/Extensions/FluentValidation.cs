using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CalculateFunding.Services.Profiling.Extensions
{
    public static class FluentValidation
    {
        public static BadRequestObjectResult AsBadRequest(this ValidationResult validationResult)
        {
            if (validationResult.IsValid)
            {
                return null;
            }

            return new BadRequestObjectResult(validationResult.AsModelStateDictionary());
        }

        public static ModelStateDictionary AsModelStateDictionary(this ValidationResult validationResult)
        {
            ModelStateDictionary modelStateDictionary = new ModelStateDictionary();

            foreach (ValidationFailure error in validationResult.Errors)
            {
                modelStateDictionary.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return modelStateDictionary;
        }
    }
}