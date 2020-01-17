using System;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Services.CosmosDbScaling.Repositories;
using CalculateFunding.Services.CosmosDbScaling.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Polly.Bulkhead;

namespace CalculateFunding.API.CosmosDbScaling
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IServiceProvider ServiceProvider { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            RegisterComponents(services);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "CosmosDbScaling Microservice API", Version = "v1" });
                c.AddSecurityDefinition("API Key", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "Ocp-Apim-Subscription-Key",
                    In = ParameterLocation.Header,
                });
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
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseMiddleware<LoggedInUserMiddleware>();

            app.UseMvc();

            app.UseHealthCheckMiddleware();
            
            app.MapWhen(
                context => !context.Request.Path.Value.StartsWith("/swagger"),
                appBuilder => appBuilder.UseMiddleware<ApiKeyMiddleware>());

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "CosmosDbScaling Microservice API");
                c.DocumentTitle = "CosmosDbScaling Microservice - Swagger";
            });
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddSingleton<ICosmosRepository, CosmosRepository>();
            builder.AddSingleton<ICosmosDbScalingService, CosmosDbScalingService>();
            builder.AddSingleton<ICosmosDbScalingRepositoryProvider, CosmosDbScalingRepositoryProvider>();
            builder.AddSingleton<ICosmosDbScalingRequestModelBuilder, CosmosDbScalingRequestModelBuilder>();
            builder.AddSingleton<ICosmosDbThrottledEventsFilter, CosmosDbThrottledEventsFilter>();
            builder.AddSingleton<IValidator<ScalingConfigurationUpdateModel>, ScalingConfigurationUpdateModelValidator>();

            builder.AddSingleton<ICosmosDbScalingConfigRepository>((ctx) =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "cosmosscalingconfig";

                CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                return new CosmosDbScalingConfigRepository(cosmosRepository);
            });

            builder.AddFeatureToggling(Configuration);
            builder.AddUserProviderFromRequest();
            builder.AddCosmosDb(Configuration);
            builder.AddServiceBus(Configuration);
            builder.AddSearch(Configuration);
            builder.AddCaching(Configuration);
            builder.AddJobsInterServiceClient(Configuration);
            builder.AddPolicySettings(Configuration);
            builder.AddSingleton<ICosmosDbScalingResiliencePolicies>(m =>
            {
                PolicySettings policySettings = builder.GetPolicySettings(Configuration);

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                CosmosDbScalingResiliencePolicies resiliencePolicies = new CosmosDbScalingResiliencePolicies()
                {
                    ScalingRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ScalingConfigRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy)
                };

                return resiliencePolicies;
            });

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsForApiApp(Configuration, "CalculateFunding.Api.CosmosDbScaling");
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Apis.CosmosDbScaling");
            builder.AddLogging("CalculateFunding.Apis.CosmosDbScaling");
            builder.AddTelemetry();
            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);
            builder.AddHttpContextAccessor();
            ServiceProvider = builder.BuildServiceProvider();
            builder.AddHealthCheckMiddleware();
        }
    }
}
