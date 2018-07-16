using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.Swagger;

namespace CalculateFunding.Api.External
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.OutputFormatters.RemoveType<StringOutputFormatter>();
                options.OutputFormatters.Add(new XmlSerializerOutputFormatter());

                var jFormatter =
                    options.OutputFormatters.FirstOrDefault(f => f.GetType() == typeof(JsonOutputFormatter)) as
                        JsonOutputFormatter;
                jFormatter?.SupportedMediaTypes.Clear();
                jFormatter?.SupportedMediaTypes.Add("text/plain");
                jFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+json");
                jFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+atom+json");

                var xFormatter =
                    options.OutputFormatters.FirstOrDefault(f => f.GetType() == typeof(XmlSerializerOutputFormatter)) as
                        XmlSerializerOutputFormatter;
                xFormatter?.SupportedMediaTypes.Clear();
                xFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+xml");
                xFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+atom+xml");
            }).AddJsonOptions(options => { options.SerializerSettings.Formatting = Formatting.Indented; })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "Calculate Funding Service API",
                    Version = "v1",
                    Description = SwaggerConstants.TopIntroductionText,
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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();
            
            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.DocExpansion(DocExpansion.List);
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = "docs";
            });
        }
    }
}