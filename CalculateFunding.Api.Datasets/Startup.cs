using AutoMapper;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Http;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.AzureStorage;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.MappingProfiles;
using CalculateFunding.Services.Datasets.Validators;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            RegisterComponents(services);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Datasets Microservice API", Version = "v1" });
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Datasets Microservice API");
                c.DocumentTitle = "Datasets Microservice - Swagger";
            });
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder
                .AddSingleton<IDefinitionsService, DefinitionsService>()
                .AddSingleton<IHealthChecker, DefinitionsService>();

            builder
                .AddSingleton<IProvidersApiClient, ProvidersApiClient>();

            builder.AddSingleton<IProviderSourceDatasetRepository, ProviderSourceDatasetRepository>(ctx => 
                new ProviderSourceDatasetRepository(CreateCosmosDbSettings("providersourcedatasets")));

            builder
                .AddSingleton<IDatasetService, DatasetService>()
                .AddSingleton<IHealthChecker, DatasetService>();

            builder
                .AddSingleton<IJobManagement, JobManagement>();

            builder
                .AddScoped<IProcessDatasetService, ProcessDatasetService>()
                .AddScoped<IHealthChecker, ProcessDatasetService>();

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
                .AddSingleton<IExcelWriter<DatasetDefinition>, DataDefinitionExcelWriter>();

            builder
              .AddSingleton<IValidator<ExcelPackage>, DatasetWorksheetValidator>();

            builder
               .AddSingleton<IDefinitionChangesDetectionService, DefinitionChangesDetectionService>();

            builder
              .AddSingleton<IDatasetDefinitionNameChangeProcessor, DatasetDefinitionNameChangeProcessor>();

            builder
                .AddSingleton<IBlobClient, BlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    Configuration.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "datasets";

                    return new BlobClient(storageSettings);
                });

            builder.AddSingleton<IProvidersResultsRepository, ProvidersResultsRepository>(ctx => 
                new ProvidersResultsRepository(CreateCosmosDbSettings("providerdatasets")));

            builder.AddSingleton<IDatasetsAggregationsRepository, DatasetsAggregationsRepository>(ctx => 
                new DatasetsAggregationsRepository(CreateCosmosDbSettings("datasetaggregations")));

            builder.AddSingleton<IVersionRepository<ProviderSourceDatasetVersion>, VersionRepository<ProviderSourceDatasetVersion>>(ctx => 
                new VersionRepository<ProviderSourceDatasetVersion>(CreateCosmosDbSettings("providersources")));

            builder.AddSingleton<IDatasetRepository, DataSetsRepository>();

            builder.AddSingleton<IDatasetSearchService, DatasetSearchService>()
                .AddSingleton<IHealthChecker, DatasetSearchService>();

            builder.AddSingleton<IDatasetDefinitionSearchService, DatasetDefinitionSearchService>();

            builder
               .AddSingleton<IDefinitionSpecificationRelationshipService, DefinitionSpecificationRelationshipService>()
               .AddSingleton<IHealthChecker, DefinitionSpecificationRelationshipService>();

            builder
               .AddSingleton<IExcelDatasetReader, ExcelDatasetReader>();

            builder
               .AddSingleton<ICalcsRepository, CalcsRepository>();

            builder
                .AddSingleton<IProviderSourceDatasetVersionKeyProvider, ProviderSourceDatasetVersionKeyProvider>();

            builder
                .AddSingleton<ICancellationTokenProvider, HttpContextCancellationProvider>();


            MapperConfiguration dataSetsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
                c.AddProfile<CalculationsMappingProfile>();
                c.AddProfile<ProviderMappingProfile>();
            });

            builder
                .AddSingleton(dataSetsConfig.CreateMapper());

            builder.AddUserProviderFromRequest();

           
            Common.Config.ApiClient.Calcs.ServiceCollectionExtensions.AddCalculationsInterServiceClient(builder, Configuration);           
            Common.Config.ApiClient.Jobs.ServiceCollectionExtensions.AddJobsInterServiceClient(builder, Configuration);
            Common.Config.ApiClient.Providers.ServiceCollectionExtensions.AddProvidersInterServiceClient(builder, Configuration);

            builder.AddCosmosDb(Configuration);

            builder.AddSearch(Configuration);
            builder
                .AddSingleton<ISearchRepository<DatasetIndex>, SearchRepository<DatasetIndex>>();
            builder
                .AddSingleton<ISearchRepository<DatasetDefinitionIndex>, SearchRepository<DatasetDefinitionIndex>>();
            builder
                .AddSingleton<ISearchRepository<DatasetVersionIndex>, SearchRepository<DatasetVersionIndex>>();

            builder.AddServiceBus(Configuration);

            builder.AddCaching(Configuration);

            builder.AddFeatureToggling(Configuration);

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsForApiApp(Configuration, "CalculateFunding.Api.Datasets");
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Datasets");
            builder.AddLogging("CalculateFunding.Api.Datasets");
            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            PolicySettings policySettings = builder.GetPolicySettings(Configuration);

            DatasetsResiliencePolicies resiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<IJobHelperResiliencePolicies>(resiliencePolicies);

            builder.AddSingleton<IDatasetsResiliencePolicies>(resiliencePolicies);

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };

            });

            builder.AddTransient<IValidator<DatasetUploadValidationModel>, DatasetItemValidator>();
            //builder.AddSpecificationsInterServiceClient(Configuration);
            Common.Config.ApiClient.Specifications.ServiceCollectionExtensions.AddSpecificationsInterServiceClient(builder, Configuration);

            builder.AddHealthCheckMiddleware();
        }

        private CosmosRepository CreateCosmosDbSettings(string containerName)
        {
            CosmosDbSettings dbSettings = 
                new CosmosDbSettings {ContainerName = containerName};

            Configuration.Bind("CosmosDbSettings", dbSettings);

            return new CosmosRepository(dbSettings);
        }

        private static DatasetsResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            return new DatasetsResiliencePolicies
            {
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CacheProviderRepository = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                ProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                ProviderRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                DatasetRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                DatasetSearchService = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                DatasetDefinitionSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };
        }
    }
}
