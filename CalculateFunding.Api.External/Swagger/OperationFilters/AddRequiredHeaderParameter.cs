using System.Collections.Generic;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger.OperationFilters
{
    public class AddRequiredHeaderParameters : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<IParameter>();


            operation.Parameters.Add(new NonBodyParameter
            {
                Name = "Accept",
                In = "header",
                Type = "string",
                Required = true,
                Default = "application/vnd.sfa.allocation.1+json",
                Description = "The calculate funding service uses the Media Type provided in the Accept header to determine what representation of a particular resources to serve. In particular this includes the version of the resource and the wire format."
            });

            operation.Parameters.Add(new NonBodyParameter
            {
                Name = "If-None-Match",
                In = "header",
                Type = "string",
                Required = false,

                Description = "if a previously provided ETag value is provided, the service will return a 304 Not Modified response is the resource has not changed."
            });
        }

    }
}
