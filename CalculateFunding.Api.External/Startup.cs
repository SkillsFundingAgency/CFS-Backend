using System;
using System.Linq;
using AutoMapper;
using CalculateFunding.Api.External.MappingProfiles;
using CalculateFunding.Api.External.Middleware;
using CalculateFunding.Api.External.Swagger;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Specs.Messages;
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
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.Validators;
using CalculateFunding.Services.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly.Bulkhead;
using Serilog;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using ISpecificationsRepository = CalculateFunding.Services.Results.Interfaces.ISpecificationsRepository;
using SpecificationsRepository = CalculateFunding.Services.Results.Repositories.SpecificationsRepository;

namespace CalculateFunding.Api.External
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
            IConfigurationSection azureADConfig = Configuration.GetSection("AzureAD");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"{azureADConfig.GetValue<string>("Authority")}/{azureADConfig.GetValue<string>("TenantId")}/";
                    options.Audience = azureADConfig.GetValue<string>("Audience");
                });

            services.AddVersionedApiExplorer(
                options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            services.AddMvc(options =>
            {
                options.OutputFormatters.RemoveType<StringOutputFormatter>();
                options.OutputFormatters.Add(new XmlSerializerOutputFormatter());

                JsonOutputFormatter jFormatter =
                    options.OutputFormatters.FirstOrDefault(f => f.GetType() == typeof(JsonOutputFormatter)) as
                        JsonOutputFormatter;
                jFormatter?.SupportedMediaTypes.Clear();
                jFormatter?.SupportedMediaTypes.Add("application/atom+json");
                jFormatter?.SupportedMediaTypes.Add("application/json");

                XmlSerializerOutputFormatter xFormatter =
                    options.OutputFormatters.FirstOrDefault(f => f.GetType() == typeof(XmlSerializerOutputFormatter)) as
                        XmlSerializerOutputFormatter;
                xFormatter?.SupportedMediaTypes.Clear();
                xFormatter?.SupportedMediaTypes.Add("application/atom+xml");
                xFormatter?.SupportedMediaTypes.Add("application/xml");
            }).AddJsonOptions(options => { options.SerializerSettings.Formatting = Formatting.Indented; })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddApiVersioning(
                o =>
                {
                    o.ReportApiVersions = true;
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.DefaultApiVersion = new ApiVersion(1, 0);
                });

            RegisterComponents(services);

            SwaggerSetup.ConfigureSwaggerServices(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseAuthentication();
            app.UseMiddleware<ContentTypeCheckMiddleware>();

            app.UseMvc();
            SwaggerSetup.ConfigureSwagger(app, provider);

            app.UseHealthCheckMiddleware();
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder.AddFeatureToggling(Configuration);

            // Register v1 services
            builder
               .AddSingleton<V1.Interfaces.IAllocationNotificationFeedsService, V1.Services.AllocationNotificationFeedsService>();
            builder
               .AddSingleton<V1.Interfaces.IProviderResultsService, V1.Services.ProviderResultsService>();
            builder
                .AddSingleton<V1.Interfaces.IAllocationsService, V1.Services.AllocationsService>();
            builder
                .AddSingleton<V1.Interfaces.ITimePeriodsService, V1.Services.TimePeriodsService>();
            builder
                .AddSingleton<V1.Interfaces.IFundingStreamService, V1.Services.FundingStreamService>();

            // Register v2 services
            builder
                .AddSingleton<V2.Interfaces.IAllocationNotificationFeedsService, V2.Services.AllocationNotificationFeedsService>();
            builder
               .AddSingleton<V2.Interfaces.IProviderResultsService, V2.Services.ProviderResultsService>();
            builder
                .AddSingleton<V2.Interfaces.IAllocationsService, V2.Services.AllocationsService>();
            builder
                .AddSingleton<V2.Interfaces.ITimePeriodsService, V2.Services.TimePeriodsService>();
            builder
                .AddSingleton<V2.Interfaces.IFundingStreamService, V2.Services.FundingStreamService>();

            // Register dependencies
            builder
                .AddSingleton<IAllocationNotificationsFeedsSearchService, AllocationNotificationsFeedsSearchService>()
                .AddSingleton<IHealthChecker, AllocationNotificationsFeedsSearchService>();

            builder
                .AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>();
            builder
               .AddSingleton<IPublishedResultsService, PublishedResultsService>()
               .AddSingleton<IHealthChecker, PublishedResultsService>();

            builder
                .AddSingleton<ICalculationProviderResultsSearchService, CalculationProviderResultsSearchService>()
                .AddSingleton<IHealthChecker, CalculationProviderResultsSearchService>();
            builder.AddSingleton<IProviderImportMappingService, ProviderImportMappingService>();

            builder
               .AddSingleton<IAllocationNotificationsFeedsSearchService, AllocationNotificationsFeedsSearchService>();

            MapperConfiguration resultsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
                c.AddProfile<ExternalApiMappingProfile>();
            });

            builder
                .AddSingleton(resultsConfig.CreateMapper());

            builder.AddSingleton<IVersionRepository<SpecificationVersion>, VersionRepository<SpecificationVersion>>((ctx) =>
            {
                CosmosDbSettings specsVersioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", specsVersioningDbSettings);

                specsVersioningDbSettings.CollectionName = "specs";

                CosmosRepository resultsRepostory = new CosmosRepository(specsVersioningDbSettings);

                return new VersionRepository<SpecificationVersion>(resultsRepostory);
            });

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
                CosmosDbSettings repoSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", repoSettings);

                repoSettings.CollectionName = "publishedproviderchanges";

                CosmosRepository repo = new CosmosRepository(repoSettings);

                ILogger logger = ctx.GetRequiredService<ILogger>();

                return new ProviderChangesRepository(repo, logger);
            });

            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>();

            builder
               .AddSingleton<IPublishedProviderResultsAssemblerService, PublishedProviderResultsAssemblerService>();

            builder.AddSingleton<Services.Specs.Interfaces.ISpecificationsRepository, Services.Specs.SpecificationsRepository>(
                ctx =>
                {
                    CosmosDbSettings specRepoDbSettings = new CosmosDbSettings();

                    Configuration.Bind("CosmosDbSettings", specRepoDbSettings);

                    specRepoDbSettings.CollectionName = "specs";

                    CosmosRepository cosmosRepository = new CosmosRepository(specRepoDbSettings);

                    return new Services.Specs.SpecificationsRepository(cosmosRepository);
                });

            builder.AddSingleton<IVersionRepository<PublishedAllocationLineResultVersion>, VersionRepository<PublishedAllocationLineResultVersion>>((ctx) =>
            {
                CosmosDbSettings versioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", versioningDbSettings);

                versioningDbSettings.CollectionName = "publishedproviderresults";

                CosmosRepository resultsRepostory = new CosmosRepository(versioningDbSettings);

                return new VersionRepository<PublishedAllocationLineResultVersion>(resultsRepostory);
            });

            builder.AddSingleton<IPublishedAllocationLineLogicalResultVersionService>((ctx) =>
            {
                IFeatureToggle featureToggle = ctx.GetService<IFeatureToggle>();

                bool enableMajorMinorVersioning = featureToggle.IsAllocationLineMajorMinorVersioningEnabled();

                if (enableMajorMinorVersioning)
                {
                    return new PublishedAllocationLineLogicalResultVersionService();
                }
                else
                {
                    return new RedundantPublishedAllocationLineLogicalResultVersionService();
                }
            });

            builder.AddSingleton<IPublishedProviderResultsSettings, PublishedProviderResultsSettings>((ctx) =>
            {
                PublishedProviderResultsSettings settings = new PublishedProviderResultsSettings();

                Configuration.Bind("PublishedProviderResultsSettings", settings);

                return settings;
            });

            builder.AddSingleton<IFundingService, FundingService>();
            builder.AddSingleton<IValidator<PolicyCreateModel>, PolicyCreateModelValidator>();
            builder.AddSingleton<IValidator<SpecificationCreateModel>, SpecificationCreateModelValidator>();
            builder.AddSingleton<IValidator<CalculationCreateModel>, CalculationCreateModelValidator>();
            builder.AddSingleton<IValidator<AssignDefinitionRelationshipMessage>, AssignDefinitionRelationshipMessageValidator>();
            builder.AddSingleton<IValidator<SpecificationEditModel>, SpecificationEditModelValidator>();
            builder.AddSingleton<IValidator<PolicyEditModel>, PolicyEditModelValidator>();
            builder.AddSingleton<IValidator<CalculationEditModel>, CalculationEditModelValidator>();
            builder.AddSingleton<IResultsRepository, ResultsRepository>();

            builder.AddResultsInterServiceClient(Configuration);

            builder.AddUserProviderFromRequest();

            builder.AddSearch(Configuration);

            builder.AddServiceBus(Configuration);

            builder.AddCaching(Configuration);

            builder.AddApplicationInsights(Configuration, "CalculateFunding.Api.External");
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.External");
            builder.AddLogging("CalculateFunding.Api.External");
            builder.AddTelemetry();

            builder.AddSpecificationsInterServiceClient(Configuration);

            builder.AddPolicySettings(Configuration);

            builder.AddHttpContextAccessor();

            builder.AddJobsInterServiceClient(Configuration);

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
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CalculationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ProviderCalculationResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    ProviderChangesRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                };
            });
            builder.AddHealthCheckMiddleware();
            builder.AddTransient<ContentTypeCheckMiddleware>();

            ServiceProvider = builder.BuildServiceProvider();
        }
    }
}