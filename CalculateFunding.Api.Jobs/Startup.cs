using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Services.Jobs.Repositories;
using CalculateFunding.Services.Jobs.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly.Bulkhead;
using System;

namespace CalculateFunding.Api.Jobs
{
    public class Startup
    {
        private static readonly string AppConfigConnectionString = Environment.GetEnvironmentVariable("AzureConfiguration:ConnectionString");
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson(options => {
                        options.SerializerSettings.ReferenceLoopHandling =
                            Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    });

            RegisterComponents(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!string.IsNullOrEmpty(AppConfigConnectionString))
            {
                app.UseAzureAppConfiguration();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            if (Configuration.IsSwaggerEnabled())
            {
                app.ConfigureSwagger(title: "Jobs Microservice API");
            }

            app.MapWhen(
                    context => !context.Request.Path.Value.StartsWith("/swagger"),
                    appBuilder => {
                        appBuilder.UseMiddleware<ApiKeyMiddleware>();
                        appBuilder.UseHealthCheckMiddleware();
                        appBuilder.UseMiddleware<LoggedInUserMiddleware>();
                        appBuilder.UseRouting();
                        appBuilder.UseAuthentication();
                        appBuilder.UseAuthorization();
                        appBuilder.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder.AddAppConfiguration();

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder
                .AddSingleton<IJobDefinitionsService, JobDefinitionsService>()
                .AddSingleton<IHealthChecker, JobDefinitionsService>();

            builder
                .AddSingleton<IJobService, JobService>()
                .AddSingleton<IHealthChecker, JobService>();

            builder.AddHttpContextAccessor();
            

            builder
                .AddSingleton<INotificationService, NotificationService>();

            builder
                .AddSingleton<IJobManagementService, JobManagementService>()
                .AddSingleton<IHealthChecker, JobManagementService>();

            builder.
                AddSingleton<IValidator<CreateJobValidationModel>, CreateJobValidator>();

            builder
                .AddSingleton<IValidator<JobDefinition>, JobDefinitionValidator>();

            builder
                 .AddSingleton<IJobDefinitionsRepository, JobDefinitionsRepository>((ctx) =>
                 {
                     CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                     Configuration.Bind("CosmosDbSettings", cosmosDbSettings);

                     cosmosDbSettings.ContainerName = "jobdefinitions";

                     CosmosRepository jobDefinitionsCosmosRepostory = new CosmosRepository(cosmosDbSettings);

                     return new JobDefinitionsRepository(jobDefinitionsCosmosRepostory);
                 });

            builder
                .AddSingleton<IJobRepository, JobRepository>((ctx) =>
                {
                    CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                    Configuration.Bind("CosmosDbSettings", cosmosDbSettings);

                    cosmosDbSettings.ContainerName = "jobs";

                    CosmosRepository jobCosmosRepostory = new CosmosRepository(cosmosDbSettings);

                    return new JobRepository(jobCosmosRepostory);
                });

            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<JobsMappingProfile>());

            builder.AddSingleton(mappingConfig.CreateMapper());

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Jobs");
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Jobs");
            builder.AddLogging("CalculateFunding.Api.Jobs");
            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddPolicySettings(Configuration);

            builder.AddCaching(Configuration);

            builder.AddServiceBus(Configuration);

            builder.AddSingleton<IJobsResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);
                BulkheadPolicy totalNetworkRequestsPolicyNonAsync = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsNonAsyncPolicy(policySettings);
                return new ResiliencePolicies
                {
                    JobDefinitionsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    CacheProviderPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                    JobRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    MessengerServicePolicy = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddHealthCheckMiddleware();

            if (Configuration.IsSwaggerEnabled())
            {
                builder.ConfigureSwaggerServices(title: "Jobs Microservice API");
            }
        }
    }
}
