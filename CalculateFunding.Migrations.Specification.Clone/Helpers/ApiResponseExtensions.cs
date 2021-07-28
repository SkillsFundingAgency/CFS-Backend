using CalculateFunding.Common.ApiClient.Models;
using Serilog;
using System;
using System.Net;

namespace CalculateFunding.Migrations.Specification.Clone.Helpers
{
    public static class ApiResponseExtensions
    {
        public static void ValidateApiResponse<T>(this ApiResponse<T> apiResponse, ILogger logger, string errorMessage)
        {
            if (apiResponse.StatusCode != HttpStatusCode.OK)
            {
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }
    }
}
