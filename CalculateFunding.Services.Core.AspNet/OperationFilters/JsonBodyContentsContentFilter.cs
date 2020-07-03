using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Services.Core.AspNet.OperationFilters
{
    public class JsonBodyContentsContentFilter : IOperationFilter
    {
        /// <summary>
        /// Configures operations decorated with the <see cref="JsonBodyContentsAttribute" />.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <param name="context">The context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var attribute = context.MethodInfo.GetCustomAttributes(typeof(JsonBodyContentsAttribute), false).FirstOrDefault();
            if (attribute == null)
            {
                return;
            }

            operation.RequestBody = new OpenApiRequestBody() { Required = true };
            operation.RequestBody.Content.Add("application/json", new OpenApiMediaType()
            {
                Schema = new OpenApiSchema()
                {
                    Type = "string",
                    Format = "json",
                },
            });
        }
    }
}
