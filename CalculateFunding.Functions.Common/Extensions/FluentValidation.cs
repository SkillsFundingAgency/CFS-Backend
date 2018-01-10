using Microsoft.AspNetCore.Mvc.ModelBinding;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Functions.Common.Extensions
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
    }
}
