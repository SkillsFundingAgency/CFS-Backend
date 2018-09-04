using System.Linq;
using AutoMapper;
using CalculateFunding.Api.Common.Extensions;
using CalculateFunding.Api.External.MappingProfiles;
using CalculateFunding.Api.External.Swagger;
using CalculateFunding.Api.External.V1.Interfaces;
using CalculateFunding.Api.External.V1.Services;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
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
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using ISpecificationsRepository = CalculateFunding.Services.Results.Interfaces.ISpecificationsRepository;
using SpecificationsRepository = CalculateFunding.Services.Results.SpecificationsRepository;

namespace CalculateFunding.Api.External
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
            var azureADConfig = Configuration.GetSection("AzureAD");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"{azureADConfig.GetValue<string>("Authority")}/{azureADConfig.GetValue<string>("TenantId")}/";
                    options.Audience = azureADConfig.GetValue<string>("Audience");
                });

            services.AddMvcCore().AddVersionedApiExplorer(
                options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            services.AddMvc(options =>
            {
                options.OutputFormatters.RemoveType<StringOutputFormatter>();
                options.OutputFormatters.Add(new XmlSerializerOutputFormatter());

                var jFormatter =
                    options.OutputFormatters.FirstOrDefault(f => f.GetType() == typeof(JsonOutputFormatter)) as
                        JsonOutputFormatter;
                jFormatter?.SupportedMediaTypes.Clear();
                jFormatter?.SupportedMediaTypes.Add("text/plain");
                jFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+json");
                jFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+atom+json");

                var xFormatter =
                    options.OutputFormatters.FirstOrDefault(f => f.GetType() == typeof(XmlSerializerOutputFormatter)) as
                        XmlSerializerOutputFormatter;
                xFormatter?.SupportedMediaTypes.Clear();
                xFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+xml");
                xFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+atom+xml");
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
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseAuthentication();

            app.UseMvc();
            SwaggerSetup.ConfigureSwagger(app, provider);

            app.UseHealthCheckMiddleware();
		}

        public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddSingleton<IAllocationNotificationsFeedsSearchService, AllocationNotificationsFeedsSearchService>()
                .AddSingleton<IHealthChecker, AllocationNotificationsFeedsSearchService>();

            builder
               .AddSingleton<IAllocationNotificationFeedsService, AllocationNotificationFeedsService>();

            builder
               .AddSingleton<IProviderResultsService, ProviderResultsService>();

            builder
                .AddSingleton<IAllocationsService, AllocationsService>();

            builder
                .AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>();
            builder
                .AddSingleton<IResultsService, ResultsService>()
                .AddSingleton<IHealthChecker, ResultsService>();
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
				c.AddProfile<ExternalApiMappingProfile>();
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
               .AddSingleton<IPublishedProviderResultsAssemblerService, PublishedProviderResultsAssemblerService>();

            builder
              .AddSingleton<IProviderProfilingRepository, ProviderProfilingRepository>();

	        builder.AddSingleton<Services.Specs.Interfaces.ISpecificationsRepository, Services.Specs.SpecificationsRepository>(
		        ctx =>
		        {
			        CosmosDbSettings specRepoDbSettings = new CosmosDbSettings();

			        Configuration.Bind("CosmosDbSettings", specRepoDbSettings);

			        specRepoDbSettings.CollectionName = "specs";

			        CosmosRepository cosmosRepository = new CosmosRepository(specRepoDbSettings);

			        return new Services.Specs.SpecificationsRepository(cosmosRepository);
		        });

			builder.AddSingleton<ITimePeriodsService, TimePeriodsService>();
            builder.AddSingleton<IFundingStreamService, FundingStreamService>();
	        builder.AddSingleton<ISpecificationsService, SpecificationsService>();
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

            builder.AddApplicationInsightsTelemetryClient(Configuration);
            builder.AddLogging("CalculateFunding.Api.External");
            builder.AddTelemetry();

            builder.AddSpecificationsInterServiceClient(Configuration);

            builder.AddProviderProfileServiceClient(Configuration);

            builder.AddPolicySettings(Configuration);

            builder.AddHttpContextAccessor();

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
                    PublishedProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy)
                };
            });
			builder.AddHealthCheckMiddleware();
		}
    }
}