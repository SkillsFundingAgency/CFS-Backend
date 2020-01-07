using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Http;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Scenarios;
using CalculateFunding.Services.Scenarios.Interfaces;
using CalculateFunding.Services.Scenarios.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Bulkhead;

namespace CalculateFunding.Api.Scenarios
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
            app.UseMiddleware<ApiKeyMiddleware>();

            app.UseMvc();

            app.UseHealthCheckMiddleware();
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddSingleton<IScenariosRepository, ScenariosRepository>();
            builder
                .AddSingleton<IScenariosService, ScenariosService>()
                .AddSingleton<IHealthChecker, ScenariosService>();
            builder
                .AddSingleton<IScenariosSearchService, ScenariosSearchService>()
                .AddSingleton<IHealthChecker, ScenariosSearchService>();

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

                return new VersionRepository<TestScenarioVersion>(resultsRepostory);
            });

            builder
              .AddSingleton<ICalcsRepository, CalcsRepository>();

            builder
               .AddSingleton<ICancellationTokenProvider, HttpContextCancellationProvider>();

            builder.AddSingleton<IDatasetRepository, DatasetRepository>();

            builder
                .AddSingleton<IDatasetDefinitionFieldChangesProcessor, DatasetDefinitionFieldChangesProcessor>();

            builder.AddUserProviderFromRequest();

            builder.AddCalculationsInterServiceClient(Configuration);
            builder.AddSpecificationsInterServiceClient(Configuration);
            builder.AddDatasetsInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);

            builder.AddCosmosDb(Configuration);

            builder.AddSearch(Configuration);
            builder
              .AddSingleton<ISearchRepository<ScenarioIndex>, SearchRepository<ScenarioIndex>>();
            builder
             .AddSingleton<ISearchRepository<TestScenarioResultIndex>, SearchRepository<TestScenarioResultIndex>>();

            builder.AddServiceBus(Configuration);

            builder.AddCaching(Configuration);

            builder.AddFeatureToggling(Configuration);

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsForApiApp(Configuration, "CalculateFunding.Api.Scenarios");
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Scenarios");
            builder.AddLogging("CalculateFunding.Api.Scenarios");
            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            builder.AddHealthCheckMiddleware();

            builder.AddPolicySettings(Configuration);

            builder.AddSingleton<IScenariosResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                Policy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                return new ScenariosResiliencePolicies()
                {
                    CalcsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    DatasetRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ScenariosRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };
            });
        }
    }
}
