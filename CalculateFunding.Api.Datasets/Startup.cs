using AutoMapper;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Http;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.AzureStorage;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.MappingProfiles;
using CalculateFunding.Services.Datasets.Validators;
using CalculateFunding.Services.DeadletterProcessor;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using Polly.Bulkhead;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;

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
            // services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddControllers()
                .AddNewtonsoftJson();

            RegisterComponents(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

            app.ConfigureSwagger(title: "Datasets Microservice API");

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
            builder.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder
                .AddSingleton<IDefinitionsService, DefinitionsService>()
                .AddSingleton<IHealthChecker, DefinitionsService>();

            builder
                .AddSingleton<IProvidersApiClient, ProvidersApiClient>();

            builder.AddSingleton<IProviderSourceDatasetRepository, ProviderSourceDatasetRepository>(ctx =>
                new ProviderSourceDatasetRepository(CreateCosmosDbSettings("providerdatasets")));

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
                .AddSingleton<IValidator<DatasetDefinition>, DatasetDefinitionValidator>();

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
                .AddSingleton<IPolicyRepository, PolicyRepository>();

            builder
                .AddSingleton<IBlobClient, BlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    Configuration.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "datasets";

                    return new BlobClient(storageSettings);
                });

            builder.AddSingleton<IProviderSourceDatasetsRepository, ProviderSourceDatasetsRepository>(ctx =>
                new ProviderSourceDatasetsRepository(CreateCosmosDbSettings("providerdatasets")));

            builder.AddSingleton<IDatasetsAggregationsRepository, DatasetsAggregationsRepository>(ctx =>
                new DatasetsAggregationsRepository(CreateCosmosDbSettings("datasetaggregations")));

            builder.AddSingleton<IVersionRepository<ProviderSourceDatasetVersion>, VersionRepository<ProviderSourceDatasetVersion>>(ctx =>
                new VersionRepository<ProviderSourceDatasetVersion>(CreateCosmosDbSettings("providerdatasets")));

            builder.AddSingleton<IDatasetRepository, DataSetsRepository>(ctx =>
                new DataSetsRepository(CreateCosmosDbSettings("datasets")));

            builder
                .AddSingleton<IDatasetSearchService, DatasetSearchService>()
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

            builder.AddCalculationsInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);
            builder.AddProvidersInterServiceClient(Configuration);
            builder.AddPoliciesInterServiceClient(Configuration);

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

           
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Datasets");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Datasets");
            builder.AddLogging("CalculateFunding.Api.Datasets");
            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(Configuration);

            DatasetsResiliencePolicies resiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<IJobHelperResiliencePolicies>(resiliencePolicies);

            builder.AddSingleton<IDatasetsResiliencePolicies>(resiliencePolicies);

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };

            });

            builder.AddTransient<IValidator<DatasetUploadValidationModel>, DatasetItemValidator>();
            //builder.AddSpecificationsInterServiceClient(Configuration);
            Common.Config.ApiClient.Specifications.ServiceCollectionExtensions.AddSpecificationsInterServiceClient(builder, Configuration);

            builder.AddHealthCheckMiddleware();

            builder.ConfigureSwaggerServices(title: "Datasets Microservice API");
        }

        private CosmosRepository CreateCosmosDbSettings(string containerName)
        {
            CosmosDbSettings dbSettings = new CosmosDbSettings();

            Configuration.Bind("CosmosDbSettings", dbSettings);

            dbSettings.ContainerName = containerName;

            return new CosmosRepository(dbSettings);
        }

        private static DatasetsResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

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
                ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };
        }
    }
}
