using System;
using System.Threading;
using AutoMapper;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Results;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Caching;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.MappingProfiles;
using CalculateFunding.Services.CalcEngine.Validators;
using CalculateFunding.Services.Calcs.MappingProfiles;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly.Bulkhead;
using Serilog;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;

namespace CalculateFunding.Api.CalcEngine
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
            services
                .AddControllers()
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
                app.ConfigureSwagger(title: "Calc Engine Microservice API");
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

            builder.AddScoped<ICalculationEngineService, CalculationEngineService>();
            builder.AddScoped<ICalculationEngine, CalculationEngine>();
            builder.AddScoped<IAllocationFactory, AllocationFactory>();
            builder.AddScoped<IJobManagement, JobManagement>();
            builder.AddSingleton<IProviderSourceDatasetVersionKeyProvider, ProviderSourceDatasetVersionKeyProvider>();
            builder.AddSingleton<IFileSystemAccess, FileSystemAccess>();

            builder.AddSingleton<IAssemblyService, AssemblyService>();
            builder.AddSingleton<ICalculationAggregationService, CalculationAggregationService>();
            builder.AddScoped<ICalculationEnginePreviewService, CalculationEnginePreviewService>();

            builder.AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();
            builder.AddSingleton<IFileSystemCache, FileSystemCache>();

            builder.AddSingleton<IProviderSourceDatasetsRepository, ProviderSourceDatasetsRepository>((ctx) =>
            {
                CosmosDbSettings providerSourceDatasetsCosmosSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", providerSourceDatasetsCosmosSettings);

                providerSourceDatasetsCosmosSettings.ContainerName = "providerdatasets";

                CosmosRepository calcsCosmosRepository = new CosmosRepository(providerSourceDatasetsCosmosSettings, new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = 8,
                    MaxTcpConnectionsPerEndpoint = 4,
                    ConsistencyLevel = ConsistencyLevel.Eventual,
                    AllowBulkExecution = true,
                });

                ICalculatorResiliencePolicies calculatorResiliencePolicies = ctx.GetService<ICalculatorResiliencePolicies>();

                return new ProviderSourceDatasetsRepository(calcsCosmosRepository, calculatorResiliencePolicies);
            });

            builder.AddSingleton<IProviderResultCalculationsHashProvider, ProviderResultCalculationsHashProvider>();

            builder.AddSingleton<IProviderResultsRepository, ProviderResultsRepository>((ctx) =>
            {
                CosmosDbSettings calcResultsDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", calcResultsDbSettings);

                calcResultsDbSettings.ContainerName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calcResultsDbSettings, new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = 8,
                    MaxTcpConnectionsPerEndpoint = 2,
                    // MaxRetryWaitTimeOnRateLimitedRequests = new TimeSpan(0, 0, 30),
                    AllowBulkExecution = true,
                });

                ILogger logger = ctx.GetService<ILogger>();

                IProviderResultCalculationsHashProvider calculationsHashProvider = ctx.GetService<IProviderResultCalculationsHashProvider>();

                ICalculatorResiliencePolicies calculatorResiliencePolicies = ctx.GetService<ICalculatorResiliencePolicies>();

                IResultsApiClient resultsApiClient = ctx.GetService<IResultsApiClient>();

                IJobManagement jobManagement = ctx.GetService<IJobManagement>();

                return new ProviderResultsRepository(
                    calcsCosmosRepostory,
                    logger,
                    calculationsHashProvider,
                    calculatorResiliencePolicies,
                    resultsApiClient,
                    jobManagement);
            });

            builder
                .AddSingleton<IBlobContainerRepository, BlobContainerRepository>();

            builder
                .AddSingleton<ICalculationsRepository, CalculationsRepository>();

            builder
               .AddSingleton<IDatasetAggregationsRepository, DatasetAggregationsRepository>();

            builder
                .AddSingleton<ICancellationTokenProvider, InactiveCancellationTokenProvider>();

            MapperConfiguration calculationsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<CalculationsMappingProfile>();
                c.AddProfile<CalcEngineMappingProfile>();
            });

            builder
                .AddSingleton(calculationsConfig.CreateMapper());

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddCalculationsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddSpecificationsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddJobsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddPoliciesInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddResultsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddDatasetsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddProvidersInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddEngineSettings(Configuration);

            builder.AddServiceBus(Configuration, "calcengine");

            builder.AddCaching(Configuration);

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.CalcEngine");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.CalcEngine");

            builder.AddLogging("CalculateFunding.Api.CalcEngine");

            builder.AddTelemetry();

            builder.AddSearch(Configuration);
            builder
               .AddSingleton<ISearchRepository<ProviderCalculationResultsIndex>, SearchRepository<ProviderCalculationResultsIndex>>();

            builder.AddFeatureToggling(Configuration);

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(Configuration);
            CalculatorResiliencePolicies calcResiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<ICalculatorResiliencePolicies>(calcResiliencePolicies);
            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) => new JobManagementResiliencePolicies()
            {
                JobsApiClient = calcResiliencePolicies.JobsApiClient
            });

            builder.AddSingleton<IValidator<ICalculatorResiliencePolicies>, CalculatorResiliencePoliciesValidator>();
            builder.AddSingleton<ICalculationEngineServiceValidator, CalculationEngineServiceValidator>();
            builder.AddSingleton<ISpecificationAssemblyProvider, SpecificationAssemblyProvider>();
            builder.AddSingleton<IBlobClient>(ctx =>
            {
                BlobStorageOptions options = new BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", options);

                options.ContainerName = "source";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(options);
                return new BlobClient(blobContainerRepository);
            });

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            builder.AddHealthCheckMiddleware();

            if (Configuration.IsSwaggerEnabled())
            {
                builder.ConfigureSwaggerServices(title: "CalcEngine Microservice API", version: "v1");
            }
        }

        private static CalculatorResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            CalculatorResiliencePolicies resiliencePolicies = new CalculatorResiliencePolicies()
            {
                CalculationResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(),
                ProviderSourceDatasetsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(),
                CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                Messenger = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy),
                CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ResultsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
            };

            return resiliencePolicies;
        }

    }
}
