using AutoMapper;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.Common.Config.ApiClient.FundingDataZone;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Profiling;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Functions.Publishing;
using CalculateFunding.Functions.Publishing.ServiceBus;
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Functions.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.DeadletterProcessor;
using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Batches;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Helper;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.IoC;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.SqlExport;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Repositories;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Errors;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Polly.Bulkhead;
using Serilog;
using System;
using System.Threading;
using BlobClient = CalculateFunding.Services.Core.AzureStorage.BlobClient;
using CommonBlobClient = CalculateFunding.Common.Storage.BlobClient;
using IBlobClient = CalculateFunding.Services.Core.Interfaces.AzureStorage.IBlobClient;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;
using TemplateMetadataSchema10 = CalculateFunding.Common.TemplateMetadata.Schema10;
using TemplateMetadataSchema11 = CalculateFunding.Common.TemplateMetadata.Schema11;
using TemplateMetadataSchema12 = CalculateFunding.Common.TemplateMetadata.Schema12;

[assembly: FunctionsStartup(typeof(Startup))]

namespace CalculateFunding.Functions.Publishing
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

        public static IServiceProvider RegisterComponents(IServiceCollection builder,
            IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder,
            IConfigurationRoot config)
        {
            builder.AddAppConfiguration();

            builder.AddSingleton<IFundingLineRoundingSettings, FundingLineRoundingSettings>();

            builder.AddSingleton<IBatchProfilingOptions, BatchProfilingOptions>();
            builder.AddSingleton<IBatchProfilingService, BatchProfilingService>();
            builder.AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>();

            builder.AddSingleton<IReProfilingResponseMapper, ReProfilingResponseMapper>();
            builder.AddSingleton<IReProfilingRequestBuilder, ReProfilingRequestBuilder>();

            builder.AddSingleton<IBatchUploadValidationService, BatchUploadValidationService>();
            builder.AddSingleton<IBatchUploadReaderFactory, BatchUploadReaderFactory>();
            builder.AddSingleton<IValidator<BatchUploadValidationRequest>, BatchUploadValidationRequestValidation>();

            builder.AddScoped<ISqlImportContextBuilder, SqlImportContextBuilder>();
            builder.AddScoped<ISqlImporter, SqlImporter>();
            builder.AddScoped<ISqlImportService, SqlImportService>();
            builder.AddScoped<ISqlNameGenerator, SqlNameGenerator>();
            builder.AddScoped<ISqlSchemaGenerator, SqlSchemaGenerator>();
            builder.AddScoped<IQaSchemaService, QaSchemaService>();

            builder.AddScoped<IDataTableImporter, DataTableImporter>((ctx) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                config.Bind("saSql", sqlSettings);

                SqlConnectionFactory sqlConnectionFactory = new SqlConnectionFactory(sqlSettings);

                return new DataTableImporter(sqlConnectionFactory);
            });

            builder.AddScoped<IQaRepository, QaRepository>((ctx) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                config.Bind("saSql", sqlSettings);

                SqlConnectionFactory sqlConnectionFactory = new SqlConnectionFactory(sqlSettings);
                SqlPolicyFactory sqlPolicyFactory = new SqlPolicyFactory();

                return new QaRepository(sqlConnectionFactory, sqlPolicyFactory);
            });

            builder.AddSingleton<ITemplateMetadataResolver>(ctx =>
             {
                 TemplateMetadataResolver resolver = new TemplateMetadataResolver();
                 ILogger logger = ctx.GetService<ILogger>();

                 TemplateMetadataSchema10.TemplateMetadataGenerator schema10Generator = new TemplateMetadataSchema10.TemplateMetadataGenerator(logger);
                 resolver.Register("1.0", schema10Generator);

                 TemplateMetadataSchema11.TemplateMetadataGenerator schema11Generator = new TemplateMetadataSchema11.TemplateMetadataGenerator(logger);
                 resolver.Register("1.1", schema11Generator);

                 TemplateMetadataSchema12.TemplateMetadataGenerator schema12Generator = new TemplateMetadataSchema12.TemplateMetadataGenerator(logger);
                 resolver.Register("1.2", schema12Generator);

                 return resolver;
             });

            builder.AddSingleton<ICosmosRepository, CosmosRepository>();

            CosmosDbSettings settings = new CosmosDbSettings();

            config.Bind("CosmosDbSettings", settings);

            settings.ContainerName = "publishedfunding";

            builder.AddSingleton(settings);

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddFeatureManagement();

            builder.AddSingleton<IConfiguration>(ctx => config);
            builder
                .AddScoped<IPublishedFundingQueryBuilder, PublishedFundingQueryBuilder>();

            builder
                .AddSingleton<IPublishingEngineOptions>(_ => new PublishingEngineOptions(config));

            builder.AddSingleton<IFundingStreamPaymentDatesRepository, FundingStreamPaymentDatesRepository>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "profiling";

                CosmosRepository profilingCosmosRepostory = new CosmosRepository(settings);

                return new FundingStreamPaymentDatesRepository(profilingCosmosRepostory);
            });

            builder.AddSingleton<IPublishedFundingRepository, PublishedFundingRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.ContainerName = "publishedfunding";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);
                IPublishedFundingQueryBuilder publishedFundingQueryBuilder = ctx.GetService<IPublishedFundingQueryBuilder>();

                return new PublishedFundingRepository(calcsCosmosRepostory, publishedFundingQueryBuilder);
            });

            builder.AddSingleton<IPublishedFundingBulkRepository, PublishedFundingBulkRepository>((ctx) =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "publishedfunding";

                IPublishingEngineOptions publishingEngineOptions = ctx.GetService<IPublishingEngineOptions>();

                CosmosRepository calcsCosmosRepository = new CosmosRepository(settings, publishingEngineOptions.AllowBatching ? new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = publishingEngineOptions.MaxRequestsPerTcpConnectionPublishedFundingCosmosBulkOptions,
                    MaxTcpConnectionsPerEndpoint = publishingEngineOptions.MaxTcpConnectionsPerEndpointPublishedFundingCosmosBulkOptions,
                    ConsistencyLevel = ConsistencyLevel.Eventual,
                    AllowBulkExecution = true
                } : null);

                IPublishingResiliencePolicies publishingResiliencePolicies = ctx.GetService<IPublishingResiliencePolicies>();

                return new PublishedFundingBulkRepository(publishingResiliencePolicies, publishingEngineOptions, calcsCosmosRepository);
            });

            CosmosDbSettings publishedfundingCosmosSettings = new CosmosDbSettings();

            config.Bind("CosmosDbSettings", publishedfundingCosmosSettings);

            publishedfundingCosmosSettings.ContainerName = "publishedfunding";

            builder.AddSingleton(publishedfundingCosmosSettings);
            builder.AddSingleton<ICosmosRepository, CosmosRepository>();
            builder.AddCaching(config);
            builder.AddSearch(config);
            builder
              .AddSingleton<ISearchRepository<PublishedProviderIndex>, SearchRepository<PublishedProviderIndex>>();
            builder
                .AddSingleton<ISearchRepository<PublishedFundingIndex>, SearchRepository<PublishedFundingIndex>>();

            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<OnRefreshFunding>();
                builder.AddScoped<OnApproveAllProviderFunding>();
                builder.AddScoped<OnPublishAllProviderFunding>();
                builder.AddScoped<OnRunSqlImport>();
                builder.AddScoped<OnRefreshFundingFailure>();
                builder.AddScoped<OnApproveAllProviderFundingFailure>();
                builder.AddScoped<OnPublishAllProviderFundingFailure>();
                builder.AddScoped<OnPublishIntegrityCheck>();
                builder.AddScoped<OnPublishIntegrityCheckFailure>();
                builder.AddScoped<OnDeletePublishedProviders>();
                builder.AddScoped<OnDeletePublishedProvidersFailure>();
                builder.AddScoped<OnReIndexPublishedProviders>();
                builder.AddScoped<OnReIndexPublishedProvidersFailure>();
                builder.AddScoped<OnGeneratePublishedFundingCsv>();
                builder.AddScoped<OnGeneratePublishedFundingCsvFailure>();
                builder.AddScoped<OnGeneratePublishedProviderEstateCsv>();
                builder.AddScoped<OnGeneratePublishedProviderEstateCsvFailure>();
                builder.AddScoped<OnApproveBatchProviderFunding>();
                builder.AddScoped<OnApproveBatchProviderFundingFailure>();
                builder.AddScoped<OnPublishBatchProviderFunding>();
                builder.AddScoped<OnPublishBatchProviderFundingFailure>();
                builder.AddScoped<OnPublishedFundingUndo>();
                builder.AddScoped<OnBatchPublishedProviderValidation>();
                builder.AddScoped<OnBatchPublishedProviderValidationFailure>();
                builder.AddScoped<OnPublishDatasetsCopy>();
                builder.AddScoped<OnPublishDatasetsCopyFailure>();
                builder.AddScoped<OnReleaseManagementDataMigration>();
                builder.AddScoped<OnReleaseManagementDataMigrationFailure>();
                builder.AddScoped<OnReleaseProvidersToChannels>();
                builder.AddScoped<OnReleaseProvidersToChannelsFailure>();
            }

            builder.AddSingleton<ISpecificationService, SpecificationService>();
            builder.AddSingleton<IProviderService, ProviderService>();
            builder.AddSingleton<IPublishedFundingService, PublishedFundingService>();
            builder.AddSingleton<IPoliciesService, PoliciesService>();
            builder.AddSingleton<IPublishedFundingVersionDataService, PublishedFundingVersionDataService>();

            builder.AddScoped<IRefreshService, RefreshService>();
            builder.AddScoped<IVariationService, VariationService>();
            builder.AddTransient<IRecordVariationErrors, VariationErrorRecorder>();
            builder.AddTransient<IApplyProviderVariations, ProviderVariationsApplication>();
            builder.AddTransient<IDetectProviderVariations, ProviderVariationsDetection>();
            builder.AddTransient<IVariationStrategyServiceLocator, VariationStrategyServiceLocator>();
            builder.AddTransient<IVariationStrategy, ClosureVariationStrategy>();
            builder.AddTransient<IVariationStrategy, ClosureWithSuccessorVariationStrategy>();
            builder.AddTransient<IVariationStrategy, ProviderMetadataVariationStrategy>();
            builder.AddTransient<IVariationStrategy, PupilNumberSuccessorVariationStrategy>();
            builder.AddTransient<IVariationStrategy, FundingUpdatedVariationStrategy>();
            builder.AddTransient<IVariationStrategy, ProfilingUpdatedVariationStrategy>();
            builder.AddTransient<IVariationStrategy, DsgTotalAllocationChangeVariationStrategy>();
            builder.AddTransient<IVariationStrategy, ReProfilingVariationStrategy>();
            builder.AddTransient<IVariationStrategy, MidYearReProfilingVariationStrategy>();
            builder.AddTransient<IVariationStrategy, MidYearClosureReProfilingVariationStrategy>();
            builder.AddTransient<IVariationStrategy, FundingSchemaUpdatedVariationStrategy>();
            builder.AddTransient<IVariationStrategy, TemplateUpdatedVariationStrategy>();
            builder.AddTransient<IVariationStrategy, CalculationValuesUpdatedVariationStrategy>();
            builder.AddTransient<IVariationStrategy, DistributionProfileStrategy>();
            builder.AddTransient<IVariationStrategy, IndicativeToLiveVariationStrategy>();

            builder.AddSingleton<IReProfilingResponseMapper, ReProfilingResponseMapper>();
            builder.AddScoped<IApproveService, ApproveService>();
            builder.AddScoped<IDatasetsDataCopyService, DatasetsDataCopyService>();
            builder.AddSingleton<IJobTracker, JobTracker>();
            builder.AddScoped<IPublishService, PublishService>();
            builder.AddSingleton<IJobManagement, JobManagement>();
            builder.AddSingleton<ISpecificationFundingStatusService, SpecificationFundingStatusService>();
            builder.AddScoped<ICsvUtils, CsvUtils>();
            builder.AddSingleton<IFileSystemAccess, FileSystemAccess>();
            builder.AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();
            builder.AddScoped<IReApplyCustomProfiles, ReApplyCustomProfiles>();
            builder.AddScoped<IPublishedProviderErrorDetection, PublishedProviderErrorDetection>();
            builder.AddTransient<IErrorDetectionStrategyLocator, ErrorDetectionStrategyLocator>();
            builder.AddTransient<IDetectPublishedProviderErrors, FundingLineValueProfileMismatchErrorDetector>();
            builder.AddTransient<IDetectPublishedProviderErrors, TrustIdMismatchErrorDetector>();
            builder.AddTransient<IDetectPublishedProviderErrors, ProviderNotFundedErrorDetector>();
            builder.AddTransient<IDetectPublishedProviderErrors, PostPaymentOutOfScopeProviderErrorDetector>();
            builder.AddTransient<IDetectPublishedProviderErrors, ProfilingConsistencyCheckErrorDetector>();
            builder.AddTransient<IDetectPublishedProviderErrors, MultipleSuccessorErrorDetector>();
            builder.AddTransient<IDetectPublishedProviderErrors, NoApplicableVariationErrorDetector>();

            builder.AddScoped<IGeneratePublishedFundingCsvJobsCreationLocator, GeneratePublishedFundingCsvJobsCreationLocator>();
            builder.AddScoped<IGeneratePublishedFundingCsvJobsCreation, GenerateRefreshPublishedFundingCsvJobsCreation>();
            builder.AddScoped<IGeneratePublishedFundingCsvJobsCreation, GenerateApprovePublishedFundingCsvJobsCreation>();
            builder.AddScoped<IGeneratePublishedFundingCsvJobsCreation, GenerateReleasePublishedFundingCsvJobsCreation>();

            builder.AddScoped<IFundingLineCsvGenerator, FundingLineCsvGenerator>();
            builder.AddScoped<IFundingLineCsvTransform, PublishedProviderFundingLineCsvTransform>();
            builder.AddScoped<IFundingLineCsvTransform, PublishedProviderVersionFundingLineCsvTransform>();
            builder.AddScoped<IFundingLineCsvTransform, PublishedProviderDeliveryProfileFundingLineCsvTransform>();
            builder.AddScoped<IFundingLineCsvTransform, PublishedProviderVersionFundingLineProfileValuesCsvTransform>();
            builder.AddScoped<IFundingLineCsvTransform, PublishedFundingFundingLineGroupingCsvTransform>();
            builder.AddScoped<IFundingLineCsvTransform, PublishedFundingVersionFundingLineGroupingCsvTransform>();
            builder.AddScoped<IFundingLineCsvTransform, PublishedGroupsFundingLineCsvTransform>();

            builder.AddScoped<IFundingLineCsvTransformServiceLocator, FundingLineCsvTransformServiceLocator>();
            builder.AddScoped<IPublishedFundingPredicateBuilder, PublishedFundingPredicateBuilder>();

            builder.AddScoped<IFundingLineCsvBatchProcessorServiceLocator, FundingLineCsvBatchProcessorServiceLocator>();
            builder.AddScoped<IFundingLineCsvBatchProcessor, PublishedProviderCsvBatchProcessor>();
            builder.AddScoped<IFundingLineCsvBatchProcessor, PublishedProviderVersionCsvBatchProcessor>();
            builder.AddScoped<IFundingLineCsvBatchProcessor, PublishedFundingOrganisationGroupCsvBatchProcessor>();
            builder.AddScoped<IFundingLineCsvBatchProcessor, PublishedFundingVersionOrganisationGroupCsvBatchProcessor>();
            builder.AddScoped<IFundingLineCsvBatchProcessor, PublishedGroupsCsvBatchProcessor>();

            builder.AddTransient<ICreateGeneratePublishedFundingCsvJobs, GeneratePublishedFundingCsvJobCreation>();
            builder.AddScoped<IPublishedProviderEstateCsvGenerator, PublishedProviderEstateCsvGenerator>()
                .AddSingleton<IHealthChecker, PublishedProviderEstateCsvGenerator>();
            builder.AddScoped<IPublishedProviderCsvTransformServiceLocator, PublishedProviderCsvTransformServiceLocator>();
            builder.AddScoped<IPublishedProviderCsvTransform, PublishedProviderEstateCsvTransform>();
            builder.AddScoped<ICreateGeneratePublishedProviderEstateCsvJobs, CreateGeneratePublishedProviderEstateCsvJobs>();
            builder.AddScoped<IPublishedFundingCsvJobsService, PublishedFundingCsvJobsService>();

            builder
                .AddSingleton<IPublishedProviderVersioningService, PublishedProviderVersioningService>()
                .AddSingleton<IHealthChecker, PublishedProviderVersioningService>();

            builder
                .AddSingleton<IPublishedProviderStatusUpdateService, PublishedProviderStatusUpdateService>()
                .AddScoped<IRefreshStateService, RefreshStateService>()
                .AddSingleton<IHealthChecker, PublishedProviderStatusUpdateService>();

            builder
                .AddSingleton<IPublishedProviderVersionService, PublishedProviderVersionService>()
                .AddSingleton<IHealthChecker, PublishedProviderVersionService>();

            builder
                .AddSingleton<IPublishedSearchService, PublishedSearchService>()
                .AddSingleton<IHealthChecker, PublishedSearchService>();
            builder.AddSingleton<IProviderFilter, ProviderFilter>();
            builder
                .AddSingleton<IPublishedProviderStatusUpdateSettings>(_ =>
                    {
                        PublishedProviderStatusUpdateSettings settings = new PublishedProviderStatusUpdateSettings();

                        config.Bind("PublishedProviderStatusUpdateSettings", settings);

                        return settings;
                    }
                );

            builder
                .AddSingleton<Common.Storage.IBlobClient, CommonBlobClient>((ctx) =>
                {
                    BlobStorageOptions storageSettings = new BlobStorageOptions();

                    config.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "publishedproviderversions";

                    IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(storageSettings);
                    return new CommonBlobClient(blobContainerRepository);
                });

            builder
               .AddSingleton<IBlobClient, BlobClient>((ctx) =>
               {
                   AzureStorageSettings storageSettings = new AzureStorageSettings();

                   config.Bind("AzureStorageSettings", storageSettings);

                   storageSettings.ContainerName = "publishedproviderversions";

                   return new BlobClient(storageSettings);
               });

            builder.AddSingleton<IVersionRepository<PublishedProviderVersion>, VersionRepository<PublishedProviderVersion>>((ctx) =>
            {
                CosmosDbSettings publishedProviderVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", publishedProviderVersioningDbSettings);

                publishedProviderVersioningDbSettings.ContainerName = "publishedfunding";

                CosmosRepository resultsRepostory = new CosmosRepository(publishedProviderVersioningDbSettings);

                return new VersionRepository<PublishedProviderVersion>(resultsRepostory, new NewVersionBuilderFactory<PublishedProviderVersion>());
            });

            builder.AddSingleton<IVersionBulkRepository<PublishedProviderVersion>, VersionBulkRepository<PublishedProviderVersion>>((ctx) =>
            {
                CosmosDbSettings PublishedProviderVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", PublishedProviderVersioningDbSettings);

                PublishedProviderVersioningDbSettings.ContainerName = "publishedfunding";

                IPublishingEngineOptions publishingEngineOptions = ctx.GetService<IPublishingEngineOptions>();

                CosmosRepository cosmosRepository = new CosmosRepository(PublishedProviderVersioningDbSettings, publishingEngineOptions.AllowBatching ? new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = publishingEngineOptions.MaxRequestsPerTcpConnectionPublishedFundingCosmosBulkOptions,
                    MaxTcpConnectionsPerEndpoint = publishingEngineOptions.MaxTcpConnectionsPerEndpointPublishedFundingCosmosBulkOptions,
                    ConsistencyLevel = ConsistencyLevel.Eventual,
                    AllowBulkExecution = true
                } : null);

                return new VersionBulkRepository<PublishedProviderVersion>(cosmosRepository, new NewVersionBuilderFactory<PublishedProviderVersion>());
            });

            builder.AddSingleton<IVersionRepository<PublishedFundingVersion>, VersionRepository<PublishedFundingVersion>>((ctx) =>
            {
                CosmosDbSettings ProviderSourceDatasetVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", ProviderSourceDatasetVersioningDbSettings);

                ProviderSourceDatasetVersioningDbSettings.ContainerName = "publishedfunding";

                CosmosRepository cosmosRepository = new CosmosRepository(ProviderSourceDatasetVersioningDbSettings);

                return new VersionRepository<PublishedFundingVersion>(cosmosRepository, new NewVersionBuilderFactory<PublishedFundingVersion>());
            });

            builder.AddSingleton<IVersionBulkRepository<PublishedFundingVersion>, VersionBulkRepository<PublishedFundingVersion>>((ctx) =>
            {
                CosmosDbSettings PublishedFundingVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", PublishedFundingVersioningDbSettings);

                PublishedFundingVersioningDbSettings.ContainerName = "publishedfunding";

                IPublishingEngineOptions publishingEngineOptions = ctx.GetService<IPublishingEngineOptions>();

                CosmosRepository cosmosRepository = new CosmosRepository(PublishedFundingVersioningDbSettings, publishingEngineOptions.AllowBatching ? new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = publishingEngineOptions.MaxRequestsPerTcpConnectionPublishedFundingCosmosBulkOptions,
                    MaxTcpConnectionsPerEndpoint = publishingEngineOptions.MaxTcpConnectionsPerEndpointPublishedFundingCosmosBulkOptions,
                    ConsistencyLevel = ConsistencyLevel.Eventual,
                    AllowBulkExecution = true
                } : null);

                return new VersionBulkRepository<PublishedFundingVersion>(cosmosRepository, new NewVersionBuilderFactory<PublishedFundingVersion>());
            });


            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.ContainerName = "calculationresults";

                IPublishingEngineOptions publishingEngineOptions = ctx.GetService<IPublishingEngineOptions>();

                CosmosRepository calcsCosmosRepository = new CosmosRepository(calssDbSettings, new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = new TimeSpan(0, 0, 15),
                    MaxRequestsPerTcpConnection = publishingEngineOptions.MaxRequestsPerTcpConnectionCalculationsCosmosBulkOptions,
                    MaxTcpConnectionsPerEndpoint = publishingEngineOptions.MaxTcpConnectionsPerEndpointCalculationsCosmosBulkOptions,
                    ConsistencyLevel = ConsistencyLevel.Eventual,
                    AllowBulkExecution = true
                });

                return new CalculationResultsRepository(calcsCosmosRepository);
            });

            builder.AddSingleton<ICalculationResultsService, CalculationResultsService>();

            builder.AddSingleton<IPublishedProviderDataGenerator, PublishedProviderDataGenerator>();

            builder.AddSingleton<IFundingLineTotalAggregator, FundingLineTotalAggregator>();

            builder
                .AddSingleton<IProfilingService, ProfilingService>()
                .AddSingleton<IHealthChecker, ProfilingService>();

            builder.AddSingleton(new MapperConfiguration(_ =>
            {
                _.AddProfile<PublishingServiceMappingProfile>();
            }).CreateMapper());

            builder.AddSingleton<IPublishedProviderDataPopulator, PublishedProviderDataPopulator>();

            builder.AddSingleton<IPublishIntegrityCheckService, PublishIntegrityCheckService>();

            builder.AddSingleton<IPublishedProviderContentsGeneratorResolver>(ctx =>
            {
                PublishedProviderContentsGeneratorResolver resolver = new PublishedProviderContentsGeneratorResolver();

                resolver.Register("1.0", new Generators.Schema10.PublishedProviderContentsGenerator());
                resolver.Register("1.1", new Generators.Schema11.PublishedProviderContentsGenerator());
                resolver.Register("1.2", new Generators.Schema12.PublishedProviderContentsGenerator());

                return resolver;
            });

            builder.AddSingleton<IPublishedFundingContentsGeneratorResolver>(ctx =>
            {
                PublishedFundingContentsGeneratorResolver resolver = new PublishedFundingContentsGeneratorResolver();

                resolver.Register("1.0", new Generators.Schema10.PublishedFundingContentsGenerator());
                resolver.Register("1.1", new Generators.Schema11.PublishedFundingContentsGenerator());
                resolver.Register("1.2", new Generators.Schema12.PublishedFundingContentsGenerator());

                return resolver;
            });

            builder.AddSingleton<IPublishedFundingIdGeneratorResolver>(ctx =>
            {
                PublishedFundingIdGeneratorResolver resolver = new PublishedFundingIdGeneratorResolver();

                IPublishedFundingIdGenerator v10Generator = new Generators.Schema10.PublishedFundingIdGenerator();

                resolver.Register("1.0", v10Generator);
                resolver.Register("1.1", v10Generator);
                resolver.Register("1.2", v10Generator);

                return resolver;
            });

            builder.AddSingleton<IDeadletterService, DeadletterService>();

            builder.AddSingleton<IPublishedProviderReIndexerService, PublishedProviderReIndexerService>();

            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Publishing");

            builder.AddLogging("CalculateFunding.Functions.Publishing", config);

            builder.AddTelemetry();

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(config);
            ResiliencePolicies publishingResiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddServiceBus(config, "publishing");

            builder.AddPublishingServices(config);

            builder.AddSingleton<IPublishingResiliencePolicies>(publishingResiliencePolicies);

            builder.AddSpecificationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddProvidersInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddJobsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddCalculationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddPoliciesInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddFundingDataServiceInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddDatasetsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddSingleton<ITransactionResiliencePolicies>((ctx) => new TransactionResiliencePolicies()
            {
                TransactionPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings)
            });

            builder.AddSingleton<ITransactionFactory, TransactionFactory>();

            builder
                .AddProfilingInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddScoped<IPublishedFundingUndoJobService, PublishedFundingUndoJobService>();
            builder.AddScoped<IPublishedFundingUndoJobCreation, PublishedFundingUndoJobCreation>();
            builder.AddScoped<IPublishedFundingUndoTaskFactoryLocator, PublishedFundingUndoTaskFactoryLocator>();
            builder.AddSingleton<IPublishedFundingUndoTaskFactory, SoftDeletePublishedFundingUndoTaskFactory>();
            builder.AddSingleton<IPublishedFundingUndoTaskFactory, HardDeletePublishedFundingUndoTaskFactory>();
            builder.AddSingleton<IPublishedFundingUndoCosmosRepository>(ctx =>
            {
                CosmosDbSettings settings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", settings);

                settings.ContainerName = "publishedfunding";

                return new PublishedFundingUndoCosmosRepository(ctx.GetService<IPublishingResiliencePolicies>(),
                    new CosmosRepository(settings));
            });
            builder.AddSingleton<IPublishedFundingUndoBlobStoreRepository>(ctx =>
            {
                BlobStorageOptions settings = new BlobStorageOptions();

                config.Bind("AzureStorageSettings", settings);

                settings.ContainerName = "publishedproviderversions";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(settings);
                return new PublishedFundingUndoBlobStoreRepository(new CommonBlobClient(blobContainerRepository),
                    ctx.GetService<IPublishingResiliencePolicies>(),
                    ctx.GetService<ILogger>());
            });

            builder.AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>();

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            builder.AddSingleton<IPublishingV3ToSqlMigrator, PublishingV3ToSqlMigrator>();
            builder.AddSingleton<IPublishedFundingReleaseManagementMigrator, PublishedFundingReleaseManagementMigrator>();
            builder.AddSingleton<IReleaseManagementRepository, ReleaseManagementRepository>((svc) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                config.Bind("releaseManagementSql", sqlSettings);
                SqlConnectionFactory factory = new SqlConnectionFactory(sqlSettings);

                SqlPolicyFactory sqlPolicyFactory = new SqlPolicyFactory();

                ExternalApiQueryBuilder externalApiQueryBuilder = new ExternalApiQueryBuilder();
                return new ReleaseManagementRepository(factory, sqlPolicyFactory, externalApiQueryBuilder);
            });

            builder.AddReleaseManagementServices(config);

            return builder.BuildServiceProvider();
        }

        private static ResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            ResiliencePolicies resiliencePolicies = new ResiliencePolicies
            {
                CalculationResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedProviderVersionRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                SpecificationsRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedFundingRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProfilingApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                FundingFeedSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                PublishedFundingBlobRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedProviderSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                PublishedIndexSearchResiliencePolicy = PublishedIndexSearchResiliencePolicy.GeneratePublishedIndexSearch(),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                DatasetsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                FundingStreamPaymentDatesRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy()

            };

            return resiliencePolicies;
        }
    }
}