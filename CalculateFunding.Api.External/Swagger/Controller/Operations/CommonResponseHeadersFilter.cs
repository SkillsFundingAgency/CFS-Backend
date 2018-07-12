using System.Collections.Generic;
using CalculateFunding.Api.External.Swagger.Controller.Operations.Abstractions;
using CalculateFunding.Api.External.Swagger.Helpers;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger.Controller.Operations
{
    public class CommonResponseHeadersFilter : SwaggerOperationFilterApplierAbs
    {
        public override string[] GetOperationIds() => new []{"ApiAllocationsByAllocationIdGet", "ApiFunding-streamsGet"};

        public override void ApplySwaggerComponents(Operation operation, OperationFilterContext context)
        {
            AddResponseHeaders(operation);
        }

        private static void AddResponseHeaders(Operation operation)
        {
            const string etagResponseHeaderDescription = "An ETag of the resource";
            const string cacheControlDescription = "Caching information for the resource";
            const string lastModifiedDescription = "Date the resource was last modified";

            operation.Responses.TryGetValue("200", out var response);
            if (response != null)
            {
                var headers = response.Headers ?? new Dictionary<string, Header>();
                headers.Add("ETag", SwaggerHelper.GenerateHeaderResponse(etagResponseHeaderDescription, "string"));
                headers.Add("Cache-Control", SwaggerHelper.GenerateHeaderResponse(cacheControlDescription, "string"));
                headers.Add("Last-Modified", SwaggerHelper.GenerateHeaderResponse(lastModifiedDescription, "date"));
                response.Headers = headers;
            }
        }
    }
}
