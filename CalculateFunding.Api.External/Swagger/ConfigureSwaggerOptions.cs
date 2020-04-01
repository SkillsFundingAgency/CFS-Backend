using CalculateFunding.Api.External.Swagger.Helpers.Readers;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger
{
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        readonly IApiVersionDescriptionProvider provider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => this.provider = provider;

        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }
        }

        static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            string swaggerDocsTopContents = SwaggerTopContentReader.ReadContents(description.ApiVersion.MajorVersion.Value);

            OpenApiInfo info = new OpenApiInfo()
            {
                Title = "Calculate Funding Service API",
                Version = "v1",
                Description = swaggerDocsTopContents,
                Contact = new OpenApiContact
                {
                    Name = "Calculate Funding Team",
                    Email = "calculate-funding@education.gov.uk"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new System.Uri("https://opensource.org/licenses/MIT")
                }
            };

            if (description.IsDeprecated)
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }
    }
}
