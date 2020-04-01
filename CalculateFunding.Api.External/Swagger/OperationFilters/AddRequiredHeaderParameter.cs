using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger.OperationFilters
{
    public class AddRequiredHeaderParameters : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "If-None-Match",
                In = ParameterLocation.Header,               
                Required = false,

                Description = "if a previously provided ETag value is provided, the service will return a 304 Not Modified response is the resource has not changed."
            });
        }

    }
}
