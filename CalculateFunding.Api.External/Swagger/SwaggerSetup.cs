using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace CalculateFunding.Api.External.Swagger
{
    public static class SwaggerSetup
    {
        public static void ConfigureSwaggerServices(IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("apiKey", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "x-api-key",
                    In = ParameterLocation.Header
                });

                var req = new OpenApiSecurityRequirement
                {
                    { new OpenApiSecurityScheme() { Type = SecuritySchemeType.ApiKey }, new List<string>() }
                };

                c.AddSecurityRequirement(req);

                c.OperationFilter<AddResponseHeadersFilter>();

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
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
