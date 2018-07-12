using System.Collections.Generic;
using CalculateFunding.Models.External;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger
{
    public static class SwaggerHelper
    {
        public static IParameter GenerateHeaderParameter(string name, bool required, string type, string description, string from = "Header", string defaultValue = null)
        {
            IParameter parameterGenerated = new NonBodyParameter()
            {
                Name = name,
                Required = required,
                Type = type,
                Description = description,
                In = from,
                Default = defaultValue
            };
            return parameterGenerated;
        }

        public static Header GenerateHeaderResponse(string description, string type)
        {
            Header headerGenerated = new Header
            {
                Description = "An ETag of the resource",
                Type = type
            };
            return headerGenerated;
        }
    }
}
