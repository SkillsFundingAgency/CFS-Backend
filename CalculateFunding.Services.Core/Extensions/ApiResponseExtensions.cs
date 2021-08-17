using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;

namespace CalculateFunding.Services.Core.Extensions
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

            if (EqualityComparer<T>.Default.Equals(apiResponse.Content, default))
            {
                return new NotFoundObjectResult($"{entityName} returned null.");
            }

            return null;
        }

        public static ActionResult IsSuccessOrReturnFailureResultAction<T>(this ApiResponse<T> apiResponse, string entityName)
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

            if (EqualityComparer<T>.Default.Equals(apiResponse.Content, default))
            {
                return new NotFoundObjectResult($"{entityName} returned null.");
            }

            return null;
        }
    }
}
