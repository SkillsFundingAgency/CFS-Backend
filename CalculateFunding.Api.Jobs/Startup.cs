using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Services.Core.AspNet;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Polly.Bulkhead;

namespace CalculateFunding.Api.Jobs
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
            services.AddControllers()
                .AddNewtonsoftJson(options => {
                        options.SerializerSettings.ReferenceLoopHandling =
                            Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    });

            RegisterComponents(services);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Jobs Microservice API", Version = "v1" });
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
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jobs Microservice API");
                c.DocumentTitle = "Jobs Microservice - Swagger";
            });

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
            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder
                .AddSingleton<IJobDefinitionsService, JobDefinitionsService>()
                .AddSingleton<IHealthChecker, JobDefinitionsService>();

            builder
                .AddSingleton<IJobService, JobService>()
                .AddSingleton<IHealthChecker, JobService>();

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

        }
    }
}
