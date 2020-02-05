using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CodeMetadataGenerator;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.TestEngine.Interfaces;
using CalculateFunding.Services.TestEngine.MappingProfiles;
using CalculateFunding.Services.TestRunner;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Repositories;
using CalculateFunding.Services.TestRunner.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Bulkhead;

namespace CalculateFunding.Api.TestRunner
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

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TestRunner Microservice API", Version = "v1" });
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestRunner Microservice API");
                c.DocumentTitle = "TestRunner Microservice - Swagger";
            });
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder
                .AddSingleton<IBuildProjectRepository, BuildProjectRepository>();

            builder
                .AddSingleton<IGherkinParserService, GherkinParserService>();

            builder
               .AddSingleton<IGherkinParser, GherkinParser>();

            builder
                .AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();

            builder
                .AddSingleton<IStepParserFactory, StepParserFactory>();

            builder
                .AddSingleton<ITestResultsRepository, TestResultsRepository>();

            builder
                .AddSingleton<IScenariosRepository, ScenariosRepository>();

            builder
                .AddScoped<ITestEngineService, TestEngineService>()
                .AddScoped<IHealthChecker, TestEngineService>();

            builder
                .AddSingleton<ITestEngine, Services.TestRunner.TestEngine>();

            builder
               .AddSingleton<IGherkinExecutor, GherkinExecutor>();

            builder
                .AddSingleton<ICalculationsRepository, CalculationsRepository>();

            builder.AddSingleton<ICosmosRepository, CosmosRepository>();

            builder.AddSingleton<IProviderSourceDatasetsRepository, ProviderSourceDatasetsRepository>((ctx) =>
            {
                CosmosDbSettings providersDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", providersDbSettings);

                providersDbSettings.ContainerName = "providersources";

                CosmosRepository providersCosmosRepostory = new CosmosRepository(providersDbSettings);

                EngineSettings engineSettings = ctx.GetService<EngineSettings>();

                return new ProviderSourceDatasetsRepository(providersCosmosRepostory, engineSettings);
            });

            builder.AddSingleton<IProviderResultsRepository, ProviderResultsRepository>((ctx) =>
            {
                CosmosDbSettings providersDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", providersDbSettings);

                providersDbSettings.ContainerName = "calculationresults";

                CosmosRepository providersCosmosRepostory = new CosmosRepository(providersDbSettings);

                ICacheProvider cacheProvider = ctx.GetService<ICacheProvider>();

                return new ProviderResultsRepository(providersCosmosRepostory);
            });

            builder
                .AddSingleton<ITestResultsSearchService, TestResultsSearchService>()
                .AddSingleton<IHealthChecker, TestResultsSearchService>();

            builder
                .AddSingleton<ITestResultsCountsService, TestResultsCountsService>()
                .AddSingleton<IHealthChecker, TestResultsCountsService>();

            MapperConfiguration mapperConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<TestEngineMappingProfile>();               
            });

            builder
                .AddSingleton(mapperConfig.CreateMapper());

            builder
                .AddScoped<ITestResultsService, TestResultsService>()
                .AddScoped<IHealthChecker, TestResultsService>();

            builder.AddUserProviderFromRequest();

            builder.AddCosmosDb(Configuration);

            builder.AddSearch(Configuration);
            builder
                .AddSingleton<ISearchRepository<TestScenarioResultIndex>, SearchRepository<TestScenarioResultIndex>>();

            
            Common.Config.ApiClient.Calcs.ServiceCollectionExtensions.AddCalculationsInterServiceClient(builder, Configuration);           
            Common.Config.ApiClient.Specifications.ServiceCollectionExtensions.AddSpecificationsInterServiceClient(builder, Configuration);
            builder.AddScenariosInterServiceClient(Configuration);          
            Common.Config.ApiClient.Providers.ServiceCollectionExtensions.AddProvidersInterServiceClient(builder, Configuration);

            builder.AddCaching(Configuration);

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.TestEngine");
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.TestEngine");
            builder.AddLogging("CalculateFunding.Api.TestEngine");
            builder.AddTelemetry();

            builder.AddEngineSettings(Configuration);

            builder.AddPolicySettings(Configuration);

            builder.AddHttpContextAccessor();

            builder.AddFeatureToggling(Configuration);

            builder.AddSingleton((System.Func<System.IServiceProvider, ITestRunnerResiliencePolicies>)((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                Policy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                return new ResiliencePolicies()
                {
                    BuildProjectRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProviderRepository = redisPolicy,
                    ProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ProviderSourceDatasetsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ScenariosRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(new[] { totalNetworkRequestsPolicy, redisPolicy }),
                    SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    TestResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    TestResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy)
                };
            }));

            builder.AddHealthCheckMiddleware();
        }
    }
}
