using AutoMapper;
using CalculateFunding.Api.Common.Middleware;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AzureStorage;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Bulkhead;

namespace CalculateFunding.Api.Datasets
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

            app.UseMvc();
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddSingleton<IDefinitionsService, DefinitionsService>();

            builder
                .AddSingleton<IDatasetService, DatasetService>();

            builder
              .AddSingleton<IValidator<CreateNewDatasetModel>, CreateNewDatasetModelValidator>();

            builder
                .AddSingleton<IValidator<DatasetVersionUpdateModel>, DatasetVersionUpdateModelValidator>();

            builder
              .AddSingleton<IValidator<DatasetMetadataModel>, DatasetMetadataModelValidator>();

            builder
                .AddSingleton<IValidator<GetDatasetBlobModel>, GetDatasetBlobModelValidator>();

            builder
               .AddSingleton<IValidator<CreateDefinitionSpecificationRelationshipModel>, CreateDefinitionSpecificationRelationshipModelValidator>();

            builder
                .AddSingleton<IBlobClient, BlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    Configuration.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "datasets";

                    return new BlobClient(storageSettings);
                });

            builder.AddSingleton<IProvidersResultsRepository, ProvidersResultsRepository>((ctx) =>
            {
                CosmosDbSettings dbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", dbSettings);

                dbSettings.CollectionName = "providersources";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(dbSettings);

                ICacheProvider cacheProvider = ctx.GetService<ICacheProvider>();

                return new ProvidersResultsRepository(calcsCosmosRepostory, cacheProvider);
            });

            builder.AddSingleton<IDatasetRepository, DataSetsRepository>();

            builder.AddSingleton<IDatasetSearchService, DatasetSearchService>();

            builder
               .AddSingleton<IDefinitionSpecificationRelationshipService, DefinitionSpecificationRelationshipService>();

            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>();

            builder
               .AddSingleton<IExcelDatasetReader, ExcelDatasetReader>();

            builder
                .AddSingleton<IProviderRepository, ProviderRepository>();

            builder
               .AddSingleton<ICalcsRepository, CalcsRepository>();

            MapperConfiguration dataSetsConfig = new MapperConfiguration(c => c.AddProfile<DatasetsMappingProfile>());
            builder
                .AddSingleton(dataSetsConfig.CreateMapper());

            builder.AddUserProviderFromRequest();

            builder.AddCalcsInterServiceClient(Configuration);
            builder.AddResultsInterServiceClient(Configuration);
            builder.AddSpecificationsInterServiceClient(Configuration);

            builder.AddCosmosDb(Configuration);

            builder.AddSearch(Configuration);

            builder.AddServiceBus(Configuration);

            builder.AddCaching(Configuration);

            builder.AddApplicationInsightsTelemetryClient(Configuration);
            builder.AddLogging("CalculateFunding.Api.Datasets");
            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddPolicySettings(Configuration);

            builder.AddHttpContextAccessor();

            builder.AddSingleton<IDatasetsResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                Policy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                return new DatasetsResiliencePolicies()
                {
                    SpecificationsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProviderRepository = redisPolicy,
                    ProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ProviderRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    DatasetRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    DatasetSearchService = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    DatasetDefinitionSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                };
            });
        }
    }
}
