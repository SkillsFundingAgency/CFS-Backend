using AutoMapper;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Http;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Scenarios;
using CalculateFunding.Services.Scenarios.Interfaces;
using CalculateFunding.Services.Scenarios.MappingProfiles;
using CalculateFunding.Services.Scenarios.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Bulkhead;
using System;

namespace CalculateFunding.Api.Scenarios
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
               .AddNewtonsoftJson();

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
                app.ConfigureSwagger(title: "Scenarios Microservice API");
            }

            app.MapWhen(
                    context => !context.Request.Path.Value.StartsWith("/swagger"),
                    appBuilder =>
                    {
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
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddSingleton<IScenariosRepository, ScenariosRepository>((ctx) =>
            {
                CosmosDbSettings scenariosVersioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", scenariosVersioningDbSettings);

                scenariosVersioningDbSettings.ContainerName = "tests";

                CosmosRepository resultsRepostory = new CosmosRepository(scenariosVersioningDbSettings);

                return new ScenariosRepository(resultsRepostory);
            });

            builder
                .AddSingleton<IScenariosService, ScenariosService>()
                .AddSingleton<IHealthChecker, ScenariosService>();
            builder
                .AddSingleton<IScenariosSearchService, ScenariosSearchService>()
                .AddSingleton<IHealthChecker, ScenariosSearchService>();

            builder.AddSingleton<IJobManagement, JobManagement>();

            builder
                .AddSingleton<IValidator<CreateNewTestScenarioVersion>, CreateNewTestScenarioVersionValidator>();

            builder
               .AddSingleton<IDatasetDefinitionFieldChangesProcessor, DatasetDefinitionFieldChangesProcessor>();

            builder.AddSingleton<IVersionRepository<TestScenarioVersion>, VersionRepository<TestScenarioVersion>>((ctx) =>
            {
                CosmosDbSettings scenariosVersioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", scenariosVersioningDbSettings);

                scenariosVersioningDbSettings.ContainerName = "tests";

                CosmosRepository resultsRepostory = new CosmosRepository(scenariosVersioningDbSettings);

                return new VersionRepository<TestScenarioVersion>(resultsRepostory, new NewVersionBuilderFactory<TestScenarioVersion>());
            });

            builder
              .AddSingleton<ICalcsRepository, CalcsRepository>();

            builder
               .AddSingleton<ICancellationTokenProvider, HttpContextCancellationProvider>();

            builder
                .AddSingleton<IDatasetDefinitionFieldChangesProcessor, DatasetDefinitionFieldChangesProcessor>();

            MapperConfiguration scenariosConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
            });

            builder
                .AddSingleton(scenariosConfig.CreateMapper());

            builder.AddCalculationsInterServiceClient(Configuration);
            builder.AddSpecificationsInterServiceClient(Configuration);
            builder.AddDatasetsInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);

            builder.AddSearch(Configuration);
            builder
              .AddSingleton<ISearchRepository<ScenarioIndex>, SearchRepository<ScenarioIndex>>();
            builder
             .AddSingleton<ISearchRepository<TestScenarioResultIndex>, SearchRepository<TestScenarioResultIndex>>();

            builder.AddServiceBus(Configuration);

            builder.AddCaching(Configuration);

            builder.AddFeatureToggling(Configuration);

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Scenarios");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Scenarios");
            builder.AddLogging("CalculateFunding.Api.Scenarios");
            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            builder.AddHealthCheckMiddleware();

            builder.AddPolicySettings(Configuration);

            builder.AddSingleton<IScenariosResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                AsyncPolicy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                return new ScenariosResiliencePolicies()
                {
                    CalcsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    DatasetsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ScenariosRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ScenariosApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };
            });

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };

            });

            if (Configuration.IsSwaggerEnabled())
            {
                builder.ConfigureSwaggerServices(title: "Scenarios Microservice API");
            }
        }
    }
}
