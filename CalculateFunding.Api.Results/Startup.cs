using System;
using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

namespace CalculateFunding.Api.Results
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IServiceProvider ServiceProvider { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

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
            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>();
            builder
                .AddSingleton<IResultsService, ResultsService>()
                .AddSingleton<IHealthChecker, ResultsService>();
            builder
              .AddSingleton<IPublishedResultsService, PublishedResultsService>()
              .AddSingleton<IHealthChecker, PublishedResultsService>();
            builder
                .AddSingleton<IResultsSearchService, ResultsSearchService>()
                .AddSingleton<IHealthChecker, ResultsSearchService>();
            builder
                .AddSingleton<ICalculationProviderResultsSearchService, CalculationProviderResultsSearchService>()
                .AddSingleton<IHealthChecker, CalculationProviderResultsSearchService>();
            builder.AddSingleton<IProviderImportMappingService, ProviderImportMappingService>();

            builder
               .AddSingleton<IAllocationNotificationsFeedsSearchService, AllocationNotificationsFeedsSearchService>();

            MapperConfiguration resultsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
                c.AddProfile<ResultServiceMappingProfile>();
            });

            builder
                .AddSingleton(resultsConfig.CreateMapper());

            builder.AddSpecificationsInterServiceClient(Configuration);

            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.CollectionName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                return new CalculationResultsRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<IProviderSourceDatasetRepository, ProviderSourceDatasetRepository>((ctx) =>
            {
                CosmosDbSettings provDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", provDbSettings);

                provDbSettings.CollectionName = "providerdatasets";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(provDbSettings);

                return new ProviderSourceDatasetRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<IPublishedProviderResultsRepository, PublishedProviderResultsRepository>((ctx) =>
            {
                CosmosDbSettings resultsDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", resultsDbSettings);

                resultsDbSettings.CollectionName = "publishedproviderresults";

                CosmosRepository resultsRepostory = new CosmosRepository(resultsDbSettings);

                return new PublishedProviderResultsRepository(resultsRepostory);
            });

            builder.AddSingleton<IPublishedProviderCalculationResultsRepository, PublishedProviderCalculationResultsRepository>((ctx) =>
            {
                CosmosDbSettings resultsDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", resultsDbSettings);

                resultsDbSettings.CollectionName = "publishedprovidercalcresults";

                CosmosRepository resultsRepostory = new CosmosRepository(resultsDbSettings);

                return new PublishedProviderCalculationResultsRepository(resultsRepostory);
            });

            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>();

            builder
               .AddSingleton<ICalculationsRepository, CalculationsRepository>();

            builder
               .AddSingleton<IPublishedProviderResultsAssemblerService, PublishedProviderResultsAssemblerService>();

            builder.AddSingleton<IVersionRepository<PublishedAllocationLineResultVersion>, VersionRepository<PublishedAllocationLineResultVersion>>((ctx) =>
            {
                CosmosDbSettings versioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", versioningDbSettings);

                versioningDbSettings.CollectionName = "publishedproviderresults";

                CosmosRepository resultsRepostory = new CosmosRepository(versioningDbSettings);

                return new VersionRepository<PublishedAllocationLineResultVersion>(resultsRepostory);
            });

            builder.AddSingleton<IVersionRepository<PublishedProviderCalculationResultVersion>, VersionRepository<PublishedProviderCalculationResultVersion>>((ctx) =>
            {
                CosmosDbSettings versioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", versioningDbSettings);

                versioningDbSettings.CollectionName = "publishedprovidercalcresults";

                CosmosRepository resultsRepostory = new CosmosRepository(versioningDbSettings);

                return new VersionRepository<PublishedProviderCalculationResultVersion>(resultsRepostory);
            });

            builder.AddUserProviderFromRequest();

            builder.AddSearch(Configuration);

            builder.AddServiceBus(Configuration);

            builder.AddCaching(Configuration);

            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Results");
            builder.AddLogging("CalculateFunding.Api.Results");
            builder.AddTelemetry();

            builder.AddSpecificationsInterServiceClient(Configuration);

            builder.AddCalcsInterServiceClient(Configuration);

            builder.AddPolicySettings(Configuration);

            builder.AddHttpContextAccessor();

            builder.AddFeatureToggling(Configuration);

            builder.AddSingleton<IPublishedAllocationLineLogicalResultVersionService>((ctx) =>
            {
                IFeatureToggle featureToggle = ctx.GetService<IFeatureToggle>();

                bool enableAllocationLineMajorMinorVersioning = featureToggle.IsAllocationLineMajorMinorVersioningEnabled();

                if (enableAllocationLineMajorMinorVersioning)
                {
                    return new PublishedAllocationLineLogicalResultVersionService();
                }
                else
                {
                    return new RedundantPublishedAllocationLineLogicalResultVersionService();
                }
            });

            builder.AddSingleton<IResultsResilliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ResiliencePolicies()
                {
                    CalculationProviderResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    ResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    SpecificationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    AllocationNotificationFeedSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    ProviderProfilingRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PublishedProviderCalculationResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    PublishedProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    CalculationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHealthCheckMiddleware();

            ServiceProvider = builder.BuildServiceProvider();
        }
    }
}
