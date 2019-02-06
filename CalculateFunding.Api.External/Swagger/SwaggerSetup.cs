using System;
using System.IO;
using System.Reflection;
using CalculateFunding.Api.External.Swagger.Helpers.Readers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.Swagger;

namespace CalculateFunding.Api.External.Swagger
{
    public static class SwaggerSetup
    {
        static Info CreateInfoForApiVersion(ApiVersionDescription description)
        {
            string swaggerDocsTopContents = SwaggerTopContentReader.ReadContents(description.ApiVersion.MajorVersion.Value);

            Info info = new Info()
            {
                Title = "Calculate Funding Service API",
                Version = "v1",
                Description = swaggerDocsTopContents,
                Contact = new Contact
                {
                    Name = "Calculate Funding Team",
                    Email = "calculate-funding@education.gov.uk"
                },
                License = new License
                {
                    Name = "MIT License",
                    Url = "https://opensource.org/licenses/MIT"
                }
            };

            if (description.IsDeprecated)
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }

        public static void ConfigureSwaggerServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                IApiVersionDescriptionProvider provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

                foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
                {
                    c.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
                }

                c.AddSecurityDefinition("apiKey", new ApiKeyScheme
                {
                    Type = "apiKey",
                    Name = "x-api-key",
                    In = "header"
                });
                c.OperationFilter<AddResponseHeadersFilter>();
                //c.OperationFilter<AddRequiredHeaderParameters>();

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                var modelsFilePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath,
                    "CalculateFunding.Models.xml");
                c.IncludeXmlComments(modelsFilePath);
            });
        }

        public static void ConfigureSwagger(IApplicationBuilder app, IApiVersionDescriptionProvider provider)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.DocExpansion(DocExpansion.List);
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", "Calculation Funding " + description.GroupName.ToUpperInvariant());
                    options.RoutePrefix = "docs";
                }
            });
        }
    }
}
