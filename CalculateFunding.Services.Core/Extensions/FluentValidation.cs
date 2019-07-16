using Microsoft.AspNetCore.Mvc.ModelBinding;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class FluentValidation
    {
        public static BadRequestObjectResult PopulateModelState(this ValidationResult validationResult)
        {
            if (!validationResult.IsValid) {
                ModelStateDictionary modelStateDictionary = new ModelStateDictionary();

                foreach (var error in validationResult.Errors)
                    modelStateDictionary.AddModelError(error.PropertyName, error.ErrorMessage);

                return new BadRequestObjectResult(modelStateDictionary);
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
    }
}
