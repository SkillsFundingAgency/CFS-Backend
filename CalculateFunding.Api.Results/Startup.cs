﻿using System;
using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Repositories;
using CalculateFunding.Services.Results.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using Serilog;

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
            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>();
            builder
                .AddSingleton<IResultsService, ResultsService>()
                .AddSingleton<IHealthChecker, ResultsService>();
            builder
              .AddSingleton<IPublishedResultsService, PublishedResultsService>()
              .AddSingleton<IHealthChecker, PublishedResultsService>();

            builder
                .AddSingleton<IProviderCalculationResultsSearchService, ProviderCalculationResultsSearchService>()
                .AddSingleton<IHealthChecker, ProviderCalculationResultsSearchService>();

            builder
                .AddSingleton<ICalculationProviderResultsSearchService, CalculationProviderResultsSearchService>()
                .AddSingleton<IHealthChecker, CalculationProviderResultsSearchService>();

            builder
               .AddSingleton<IAllocationNotificationsFeedsSearchService, AllocationNotificationsFeedsSearchService>();

            builder.AddSingleton<IProviderVariationAssemblerService, ProviderVariationAssemblerService>();

            MapperConfiguration resultsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
                c.AddProfile<ResultServiceMappingProfile>();
            });

            builder
                .AddSingleton(resultsConfig.CreateMapper());

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

            builder.AddSingleton<IProviderChangesRepository, ProviderChangesRepository>((ctx) =>
            {
                CosmosDbSettings resultsDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", resultsDbSettings);

                resultsDbSettings.CollectionName = "publishedproviderchanges";

                CosmosRepository resultsRepostory = new CosmosRepository(resultsDbSettings);

                ILogger logger = ctx.GetService<ILogger>();

                return new ProviderChangesRepository(resultsRepostory, logger);
            });

            builder.AddSingleton<IProviderCalculationResultsReIndexerService, ProviderCalculationResultsReIndexerService>();

            builder.AddSingleton<IValidator<MasterProviderModel>, MasterProviderModelValidator>();

            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>()
                .AddSingleton<IPoliciesRepository, PoliciesRepository>();

            builder
               .AddSingleton<ICalculationsRepository, CalculationsRepository>();

            builder
               .AddSingleton<IPublishedProviderResultsAssemblerService, PublishedProviderResultsAssemblerService>();

            builder.AddSingleton<IPublishedProviderResultsSettings, PublishedProviderResultsSettings>((ctx) =>
            {
                PublishedProviderResultsSettings settings = new PublishedProviderResultsSettings();

                Configuration.Bind("PublishedProviderResultsSettings", settings);

                return settings;
            });

            builder.AddSingleton<IVersionRepository<PublishedAllocationLineResultVersion>, VersionRepository<PublishedAllocationLineResultVersion>>((ctx) =>
            {
                CosmosDbSettings versioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", versioningDbSettings);

                versioningDbSettings.CollectionName = "publishedproviderresults";

                CosmosRepository resultsRepostory = new CosmosRepository(versioningDbSettings);

                return new VersionRepository<PublishedAllocationLineResultVersion>(resultsRepostory);
            });

            builder.AddUserProviderFromRequest();

            builder.AddSearch(Configuration);

            builder.AddServiceBus(Configuration);

            builder.AddCaching(Configuration);

            builder.AddApplicationInsights(Configuration, "CalculateFunding.Api.Results");
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Results");
            builder.AddLogging("CalculateFunding.Api.Results");
            builder.AddTelemetry();

            builder.AddSpecificationsInterServiceClient(Configuration);
            builder.AddCalcsInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);
            builder.AddPoliciesInterServiceClient(Configuration);

            builder.AddPolicySettings(Configuration);

            builder.AddHttpContextAccessor();

            builder.AddFeatureToggling(Configuration);

            builder.AddSingleton<IPublishedAllocationLineLogicalResultVersionService, PublishedAllocationLineLogicalResultVersionService>();

            builder.AddSingleton<IResultsResiliencePolicies>((ctx) =>
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
                    CalculationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ProviderChangesRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ProviderCalculationResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                };
            });

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHealthCheckMiddleware();

            ServiceProvider = builder.BuildServiceProvider();
        }
    }
}
