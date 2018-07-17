using System;
using System.IO;
using System.Reflection;
using CalculateFunding.Api.External.Swagger.Helpers.Readers;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.Swagger;

namespace CalculateFunding.Api.External.Swagger
{
    public static class SwaggerSetup
    {
        public static void ConfigureSwaggerServices(IServiceCollection services)
        {
            string swaggerDocsTopContents = SwaggerTopContentReader.ReadContents();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "Calculate Funding Service API",
                    Version = "v1",
                    Description = swaggerDocsTopContents,
                    Contact = new Contact
                    {
                        Name = "Clifford Smith",
                        Email = "cliffordsmith@education.gov.uk"
                    },
                    License = new License
                    {
                        Name = "MIT License",
                        Url = "https://opensource.org/licenses/MIT"
                    }
                });
                c.AddSecurityDefinition("apiKey", new ApiKeyScheme
                {
                    Type = "apiKey",
                    Name = "x-api-key",
                    In = "header"
                });
                c.OperationFilter<AddResponseHeadersFilter>();
                c.OperationFilter<AddRequiredHeaderParameters>();

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                var modelsFilePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath,
                    "CalculateFunding.Models.xml");
                c.IncludeXmlComments(modelsFilePath);
            });
        }

        public static void ConfigureSwagger(IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.DocExpansion(DocExpansion.List);
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Calculate Funding Service API V1");
                c.RoutePrefix = "docs";
            });
        }
    }
}
