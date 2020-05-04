using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ModelStateDictionary = Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class FluentValidationExtensions
    {
        public static BadRequestObjectResult PopulateModelState(this ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                return new BadRequestObjectResult(validationResult.ToModelStateDictionary());
            }

            return null;
        }
        
        public static ModelStateDictionary ToModelStateDictionary(this ValidationResult validationResult)
        {
            if (!validationResult.IsValid)
            {
                ModelStateDictionary modelStateDictionary = new ModelStateDictionary();

                foreach (var error in validationResult.Errors)
                    modelStateDictionary.AddModelError(error.PropertyName, error.ErrorMessage);

                return modelStateDictionary;
            }

            return null;
        }
        
        public static ModelStateDictionary ToModelStateDictionary(this ModelState mvcModelState)
        {
            if (mvcModelState?.Errors != null && mvcModelState.Errors.Any())
            {
                ModelStateDictionary modelStateDictionary = new ModelStateDictionary();

                foreach (var error in mvcModelState.Errors)
                    modelStateDictionary.AddModelError("", error.ErrorMessage);

                return modelStateDictionary;
            }

            return null;
        }

        public static  BadRequestObjectResult AsBadRequest(this ValidationResult validationResult)
        {
            return validationResult.PopulateModelState();
        }

        public static IDictionary<string, string> GetResultDictionary(this ValidationResult validationResult)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!validationResult.IsValid)
            {
                foreach (ValidationFailure error in validationResult.Errors)
                    result.Add(error.PropertyName, error.ErrorMessage);
            }

            return result;
        }

        public static ValidationResult WithError(this ValidationResult validationResult, string propertyName, string error)
        {
            if (validationResult == null) return null;
            validationResult.Errors.Add(new ValidationFailure(propertyName, error));
            return validationResult;
        }
    }
}
