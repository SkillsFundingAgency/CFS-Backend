using System.Collections.Generic;
using System.Net;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Results
{
    public static class ApiResponseExtensions
    {
        public static IActionResult IsSuccessOrReturnFailureResult<T>(this ApiResponse<T> apiResponse, string entityName)
        {
            Guard.IsNullOrWhiteSpace(entityName, nameof(entityName));

            if (apiResponse == null)
            {
                return new InternalServerErrorResult($"{entityName} API response returned null.");
            }

            if (apiResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return new NotFoundObjectResult($"{entityName} not found.");
            }

            if (apiResponse.StatusCode != HttpStatusCode.OK)
            {
                return new InternalServerErrorResult($"{entityName} API call did not return success, but instead '{apiResponse.StatusCode}'");
            }

            if (EqualityComparer<T>.Default.Equals(apiResponse.Content, default(T)))
            {
                return new NotFoundObjectResult($"{entityName} returned null.");
            }

            return null;
        }
    }
}