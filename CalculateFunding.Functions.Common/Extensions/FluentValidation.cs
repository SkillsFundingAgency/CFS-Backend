using Microsoft.AspNetCore.Mvc.ModelBinding;
using FluentValidation.Results;

namespace CalculateFunding.Functions.Common.Extensions
{
    public static class FluentValidation
    {
        public static void PopulateModelState(this ValidationResult validationResult, ModelStateDictionary modelStateDictionary)
        {
            foreach (var error in validationResult.Errors)
                modelStateDictionary.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
