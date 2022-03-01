using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.AspNet.OperationFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.IO;
using System.Reflection;

namespace CalculateFunding.Services.Core.AspNet.Extensions
{
    public static class SwaggerSetup
    {
        public static void ConfigureSwaggerServices(this IServiceCollection services, string title = null, string version = "v1", Action<SwaggerGenOptions> setupSecurity = null)
        {
            services.AddSwaggerGen(c =>
            {
                if (!string.IsNullOrWhiteSpace(title))
                {
                    c.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version });
                }

                if (setupSecurity != null)
                {
                    setupSecurity(c);
                }
                else
                {
                    c.AddSecurityDefinition("API Key", new OpenApiSecurityScheme()
                    {
                        Type = SecuritySchemeType.ApiKey,
                        Name = "Ocp-Apim-Subscription-Key",
                        In = ParameterLocation.Header,
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "API Key"
                                }
                            },
                            new string[] { }
                        }
                    });
                }

                c.OperationFilter<AddResponseHeadersFilter>();

                var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                c.EnableAnnotations();

                c.OperationFilter<JsonBodyContentsContentFilter>();
            });

            services.AddSwaggerGenNewtonsoftSupport();
        }

        public static void ConfigureSwagger(this IApplicationBuilder app, IApiVersionDescriptionProvider provider = null, string title = null, string version = "v1")
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    Guard.ArgumentNotNull(provider, nameof(provider));

                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.DocExpansion(DocExpansion.List);
                        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", "Calculation Funding " + description.GroupName.ToUpperInvariant());
                        options.RoutePrefix = "docs";
                    }
                }
                else
                {
                    Guard.IsNullOrWhiteSpace(title, nameof(title));

                    options.SwaggerEndpoint($"/swagger/{version}/swagger.json", title);
                    options.DocumentTitle = title;
                }
            });
        }

        public static bool IsSwaggerEnabled(this IConfiguration configuration) => configuration.GetValue("FeatureManagement:EnableSwagger", false);
    }
}
