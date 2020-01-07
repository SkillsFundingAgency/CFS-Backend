using System;
using System.IO;
using System.Reflection;
using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.FundingPolicy;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.MappingProfiles;
using CalculateFunding.Services.Policy.Validators;
using CalculateFunding.Services.Providers.Validators;
using FluentValidation;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using TemplateMetadataSchema10 = CalculateFunding.Common.TemplateMetadata.Schema10;

namespace CalculateFunding.Api.Policy
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            RegisterComponents(services);

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Funding Service Policy API", Version = "v1" });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();

                app.UseMiddleware<ApiKeyMiddleware>();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Policy API V1");
                c.RoutePrefix = string.Empty;
            });

            app.UseHttpsRedirection();

            app.UseMiddleware<LoggedInUserMiddleware>();

            app.UseMvc();

            app.UseHealthCheckMiddleware();
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder
                .AddSingleton<IFundingStreamService, FundingStreamService>()
                .AddSingleton<IHealthChecker, FundingStreamService>()
                .AddSingleton<IValidator<FundingStreamSaveModel>, FundingStreamSaveModelValidator>();

            builder
                .AddSingleton<IFundingPeriodService, FundingPeriodService>()
                .AddSingleton<IHealthChecker, FundingPeriodService>()
                .AddSingleton<IFundingPeriodValidator, FundingPeriodValidator>();

            builder
                .AddSingleton<IFundingSchemaService, FundingSchemaService>()
                .AddSingleton<IHealthChecker, FundingSchemaService>();

            builder
                .AddSingleton<IFundingConfigurationService, FundingConfigurationService>()
                .AddSingleton<IHealthChecker, FundingConfigurationService>();

            builder
                .AddSingleton<IFundingTemplateService, FundingTemplateService>()
                .AddSingleton<IHealthChecker, FundingTemplateService>();

            builder
                .AddSingleton<IFundingTemplateValidationService, FundingTemplateValidationService>()
                .AddSingleton<IHealthChecker, FundingTemplateValidationService>();

            builder
                .AddSingleton<IFundingSchemaRepository, FundingSchemaRepository>((ctx) =>
                {
                    BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                    Configuration.Bind("AzureStorageSettings", blobStorageOptions);

                    blobStorageOptions.ContainerName = "fundingschemas";

                    return new FundingSchemaRepository(blobStorageOptions);
                });

            builder
               .AddSingleton<IFundingTemplateRepository, FundingTemplateRepository>((ctx) =>
               {
                   BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                   Configuration.Bind("AzureStorageSettings", blobStorageOptions);

                   blobStorageOptions.ContainerName = "fundingtemplates";

                   return new FundingTemplateRepository(blobStorageOptions);
               });

            builder
             .AddSingleton<IPolicyRepository, PolicyRepository>((ctx) =>
             {
                 CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                 cosmosDbSettings.ContainerName = "policy";

                 Configuration.Bind("CosmosDbSettings", cosmosDbSettings);

                 CosmosRepository cosmosRepostory = new CosmosRepository(cosmosDbSettings);

                 return new PolicyRepository(cosmosRepostory);
             })
             .AddSingleton<IHealthChecker, FundingStreamService>();

            builder.AddSingleton<IPolicyResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                Polly.Policy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                return new PolicyResiliencePolicies()
                {
                    PolicyRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProvider = redisPolicy,
                    FundingSchemaRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    FundingTemplateRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };
            });

            builder.AddSingleton<ITemplateMetadataResolver>((ctx) =>
            {
                TemplateMetadataResolver resolver = new TemplateMetadataResolver();

                TemplateMetadataSchema10.TemplateMetadataGenerator schema10Generator = new TemplateMetadataSchema10.TemplateMetadataGenerator(ctx.GetService<ILogger>());

                resolver.Register("1.0", schema10Generator);

                return resolver;
            });

            builder.AddSingleton<IValidator<FundingConfiguration>, SaveFundingConfigurationValidator>();
            builder.AddSingleton<IValidator<FundingPeriodsJsonModel>, FundingPeriodJsonModelValidator>();

            builder.AddPolicySettings(Configuration);

            MapperConfiguration fundingConfMappingConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<FundingConfigurationMappingProfile>();
            });

            builder
                .AddSingleton(fundingConfMappingConfig.CreateMapper());

            builder.AddCaching(Configuration);

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsForApiApp(Configuration, "CalculateFunding.Api.Policy");
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Policy");
            builder.AddLogging("CalculateFunding.Api.Policy");
            builder.AddTelemetry();            

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            builder.AddHealthCheckMiddleware();
        }
    }
}
