using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Extensions;
using Serilog;
using System;
using System.Net;

namespace CalculateFunding.Migrations.Specification.Clone.Helpers
{
    public static class ApiResponseExtensions
    {
        public static void ValidateApiResponse<T>(this ValidatedApiResponse<T> apiResponse, ILogger logger, string errorMessage)
        {
            if (apiResponse.StatusCode != HttpStatusCode.OK)
            {
                errorMessage = $"ErrorMessage={errorMessage} StatusCode={apiResponse.StatusCode} ModelState={apiResponse.ModelState.AsJson()}";

                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }

        public static void ValidateApiResponse<T>(this ApiResponse<T> apiResponse, ILogger logger, string errorMessage)
        {
            if (apiResponse.StatusCode != HttpStatusCode.OK)
            {
                errorMessage = $"ErrorMessage={errorMessage} StatusCode={apiResponse.StatusCode}";

                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }
    }
}
