using System;
using System.Threading;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Functions.Datasets.ServiceBus;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Functions.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Helpers;
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
using CalculateFunding.Services.DeadletterProcessor;
using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Repositories;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using Polly.Bulkhead;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;
using BlobClient = CalculateFunding.Common.Storage.BlobClient;
using IBlobClient = CalculateFunding.Common.Storage.IBlobClient;
using LocalBlobClient = CalculateFunding.Services.Core.AzureStorage.BlobClient;
using LocalIBlobClient = CalculateFunding.Services.Core.Interfaces.AzureStorage.IBlobClient;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Datasets.Converter;
using CalculateFunding.Services.Core.Caching.FileSystem;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Datasets.Startup))]

namespace CalculateFunding.Functions.Datasets
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services, builder.GetFunctionsConfigurationToIncludeHostJson());
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfiguration azureFuncConfig = null)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig(azureFuncConfig);
            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddAppConfiguration();
            builder.AddSingleton<IConfiguration>(config);

            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<OnDataDefinitionChanges>();
                builder.AddScoped<OnDatasetEvent>();
                builder.AddScoped<OnDatasetValidationEvent>();
                builder.AddScoped<OnDatasetEventFailure>();
                builder.AddScoped<OnDatasetValidationEventFailure>();
                builder.AddScoped<OnMapFdzDatasetsEventFired>();
                builder.AddScoped<OnMapFdzDatasetsEventFiredFailure>();
                builder.AddScoped<OnDeleteDatasets>();
                builder.AddScoped<OnDeleteDatasetsFailure>();
                builder.AddScoped<OnRunConverterDataMerge>();
                builder.AddScoped<OnRunConverterDataMergeFailure>();
                builder.AddScoped<OnCreateSpecificationConverterDatasetsMerge>();
                builder.AddScoped<OnCreateSpecificationConverterDatasetsMergeFailure>();
                builder.AddScoped<OnConverterWizardActivityCsvGeneration>();
                builder.AddScoped<OnConverterWizardActivityCsvGenerationFailure>();
            }

            builder.AddSingleton<ISpecificationConverterDataMerge, SpecificationConverterDataMerge>();
            builder.AddSingleton<IConverterDataMergeService, ConverterDataMergeService>();
            builder.AddSingleton<IDatasetCloneBuilderFactory, DatasetCloneBuilderFactory>();
            builder.AddSingleton<IConverterDataMergeLogger, ConverterDataMergeLogger>();
            builder.AddSingleton<IConverterEligibleProviderService, ConverterEligibleProviderService>();
            builder.AddSingleton<IConverterWizardActivityCsvGenerationGeneratorService, ConverterWizardActivityCsvGenerationGeneratorService>();
            builder.AddSingleton<IFileSystemAccess, FileSystemAccess>();
            builder.AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();
            builder.AddSingleton<ICsvUtils, CsvUtils>();
            builder.AddSingleton<IConverterWizardActivityToCsvRowsTransformation, ConverterWizardActivityToCsvRowsTransformation>();
            builder.AddSingleton<IValidator<ConverterMergeRequest>, ConverterMergeRequestValidation>();
            builder.AddSingleton<IDatasetIndexer, DatasetIndexer>();

            builder.AddSingleton<IDateTimeProvider, DateTimeProvider>();

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
               .AddSingleton<IDefinitionsService, DefinitionsService>();

            builder
                .AddSingleton<IProvidersApiClient, ProvidersApiClient>();

            builder.AddSingleton<IProviderSourceDatasetRepository, ProviderSourceDatasetRepository>(ctx =>
                new ProviderSourceDatasetRepository(CreateCosmosDbSettings(config, "providerdatasets")));

            builder
                .AddSingleton<IDatasetService, DatasetService>();

            builder
                .AddSingleton<IDatasetDataMergeService, DatasetDataMergeService>();

            builder
                .AddSingleton<IJobManagement, JobManagement>();

            builder
                .AddSingleton<IDeadletterService, DeadletterService>();

            builder
                .AddScoped<IProcessDatasetService, ProcessDatasetService>();

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
               .AddSingleton<IExcelDatasetWriter, DataDefinitionExcelWriter>();

            builder
                .AddSingleton<IValidator<ExcelPackage>, DatasetWorksheetValidator>();

            builder
                .AddSingleton<IValidator<DatasetDefinition>, DatasetDefinitionValidator>();

            builder
                .AddSingleton<IDefinitionChangesDetectionService, DefinitionChangesDetectionService>();

            builder
                .AddScoped<IDatasetDefinitionNameChangeProcessor, DatasetDefinitionNameChangeProcessor>();

            builder
                .AddSingleton<IValidator<CreateDatasetDefinitionFromTemplateModel>, CreateDatasetDefinitionFromTemplateModelValidator>();

            builder
                .AddSingleton<IPolicyRepository, PolicyRepository>();

            builder
                .AddSingleton<IBlobClient, BlobClient>((ctx) =>
                {
                    BlobStorageOptions storageSettings = new BlobStorageOptions();

                    config.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "datasets";

                    IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);
                    return new BlobClient(blobContainerRepository);
                });

            builder
                .AddSingleton<LocalIBlobClient, LocalBlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    config.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "datasets";

                    return new LocalBlobClient(storageSettings);
                });

            builder.AddSingleton<IProviderSourceDatasetsRepository, ProviderSourceDatasetsRepository>(ctx =>
                new ProviderSourceDatasetsRepository(CreateCosmosDbSettings(config, "providerdatasets")));

            builder.AddSingleton<IDatasetRepository, DataSetsRepository>(ctx =>
            {
                return new DataSetsRepository(CreateCosmosDbSettings(config, "datasets"));
            });

            builder.AddSingleton<IDatasetSearchService, DatasetSearchService>();

            builder.AddSingleton<IProviderSourceDatasetVersionKeyProvider, ProviderSourceDatasetVersionKeyProvider>();

            builder.AddSingleton<IDatasetDefinitionSearchService, DatasetDefinitionSearchService>();

            builder
               .AddSingleton<IDefinitionSpecificationRelationshipService, DefinitionSpecificationRelationshipService>();

            builder
               .AddSingleton<IExcelDatasetReader, ExcelDatasetReader>();

            builder
               .AddSingleton<ICalcsRepository, CalcsRepository>();

            builder.AddTransient<IValidator<DatasetUploadValidationModel>, DatasetUploadValidationModelValidator>();

            MapperConfiguration dataSetsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
                c.AddProfile<CalculationsMappingProfile>();
                c.AddProfile<ProviderMappingProfile>();
            });

            builder
                .AddSingleton(dataSetsConfig.CreateMapper());

            builder.AddSingleton<IVersionRepository<ProviderSourceDatasetVersion>, VersionRepository<ProviderSourceDatasetVersion>>(ctx =>
                new VersionRepository<ProviderSourceDatasetVersion>(CreateCosmosDbSettings(config, "providerdatasets"), new NewVersionBuilderFactory<ProviderSourceDatasetVersion>()));

            builder.AddSingleton<IDatasetsAggregationsRepository, DatasetsAggregationsRepository>(ctx =>
                new DatasetsAggregationsRepository(CreateCosmosDbSettings(config, "datasetaggregations")));

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            builder.AddCalculationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddSpecificationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddJobsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddProvidersInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddPoliciesInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddSearch(config);
            builder
                .AddSingleton<ISearchRepository<DatasetIndex>, SearchRepository<DatasetIndex>>();
            builder
                .AddSingleton<ISearchRepository<DatasetDefinitionIndex>, SearchRepository<DatasetDefinitionIndex>>();
            builder
                .AddSingleton<ISearchRepository<DatasetVersionIndex>, SearchRepository<DatasetVersionIndex>>();

            builder.AddServiceBus(config, "datasets");

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Datasets");
            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Datasets");
            builder.AddLogging("CalculateFunding.Functions.Datasets");
            builder.AddTelemetry();

            builder.AddFeatureToggling(config);

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(config);

            DatasetsResiliencePolicies resiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<IDatasetsResiliencePolicies>(resiliencePolicies);

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };

            });
            
            builder.AddSingleton<IVersionBulkRepository<ProviderSourceDatasetVersion>, VersionBulkRepository<ProviderSourceDatasetVersion>>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "providerdatasets";

                CosmosRepository cosmos = new CosmosRepository(settings, new CosmosClientOptions
                {
                    AllowBulkExecution = true
                });

                return new VersionBulkRepository<ProviderSourceDatasetVersion>(cosmos, new NewVersionBuilderFactory<ProviderSourceDatasetVersion>());
            });
            builder.AddSingleton<IProviderSourceDatasetBulkRepository, ProviderSourceDatasetBulkRepository>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "providerdatasets";

                CosmosRepository cosmos = new CosmosRepository(settings, new CosmosClientOptions
                {
                    AllowBulkExecution = true
                });

                return new ProviderSourceDatasetBulkRepository(cosmos);
            });

            return builder.BuildServiceProvider();
        }

        private static CosmosRepository CreateCosmosDbSettings(IConfigurationRoot config, string containerName)
        {
            CosmosDbSettings dbSettings = new CosmosDbSettings();

            config.Bind("CosmosDbSettings", dbSettings);

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
                DatasetVersionSearchService = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                DatasetDefinitionSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };
        }
    }
}
