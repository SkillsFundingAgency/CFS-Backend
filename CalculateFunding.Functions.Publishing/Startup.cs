using System;
using System.Threading;
using AutoMapper;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Profiling;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Functions.Publishing;
using CalculateFunding.Functions.Publishing.ServiceBus;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.DeadletterProcessor;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.Helper;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.IoC;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Repositories;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Errors;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Polly.Bulkhead;
using Serilog;
using BlobClient = CalculateFunding.Services.Core.AzureStorage.BlobClient;
using IBlobClient = CalculateFunding.Services.Core.Interfaces.AzureStorage.IBlobClient;
using CommonBlobClient = CalculateFunding.Common.Storage.BlobClient;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;

[assembly: FunctionsStartup(typeof(Startup))]

namespace CalculateFunding.Functions.Publishing
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

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
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddFeatureManagement();

            builder.AddSingleton<IConfiguration>(ctx => config);
            builder
                .AddScoped<IPublishedFundingQueryBuilder, PublishedFundingQueryBuilder>();

            builder.AddSingleton<IPublishedFundingRepository, PublishedFundingRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.ContainerName = "publishedfunding";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);
                IPublishedFundingQueryBuilder publishedFundingQueryBuilder = ctx.GetService<IPublishedFundingQueryBuilder>();

                return new PublishedFundingRepository(calcsCosmosRepostory, publishedFundingQueryBuilder);
            });

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
                builder.AddScoped(_ =>
                {
                    return new OnRefreshFunding(_.GetService<ILogger>(),
                        _.GetService<IRefreshService>(),
                        _.GetService<IMessengerService>(),
                        _.GetService<IUserProfileProvider>(),
                        true);
                });
                builder.AddScoped(_ =>
                {
                    return new OnApproveAllProviderFunding(_.GetService<ILogger>(),
                        _.GetService<IApproveService>(),
                        _.GetService<IMessengerService>(),
                        _.GetService<IUserProfileProvider>(),
                        true);
                });
                builder.AddScoped(_ =>
                {
                    return new OnPublishAllProviderFunding(_.GetService<ILogger>(),
                        _.GetService<IPublishService>(),
                        _.GetService<IMessengerService>(),
                        _.GetService<IUserProfileProvider>(),
                        true);
                });
                builder.AddScoped<OnRefreshFundingFailure>();
                builder.AddScoped<OnApproveAllProviderFundingFailure>();
                builder.AddScoped<OnPublishAllProviderFundingFailure>();
                builder.AddScoped<OnDeletePublishedProviders>();
                builder.AddScoped<OnReIndexPublishedProviders>();
                builder.AddScoped<OnGeneratePublishedFundingCsv>();
                builder.AddScoped<OnGeneratePublishedFundingCsvFailure>();
                builder.AddScoped<OnGeneratePublishedProviderEstateCsv>();
                builder.AddScoped<OnGeneratePublishedProviderEstateCsvFailure>();
                builder.AddScoped(_ =>
                {
                    return new OnApproveBatchProviderFunding(_.GetService<ILogger>(),
                        _.GetService<IApproveService>(),
                        _.GetService<IMessengerService>(),
                        _.GetService<IUserProfileProvider>(),
                        true);
                });
                builder.AddScoped<OnApproveBatchProviderFundingFailure>();
                builder.AddScoped(_ =>
                {
                    return new OnPublishBatchProviderFunding(_.GetService<ILogger>(),
                        _.GetService<IPublishService>(),
                        _.GetService<IMessengerService>(),
                        _.GetService<IUserProfileProvider>(),
                        true);
                });
                builder.AddScoped<OnPublishBatchProviderFundingFailure>();
                builder.AddScoped<OnPublishedFundingUndo>();
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
            builder.AddScoped<IApproveService, ApproveService>();
            builder.AddSingleton<IJobTracker, JobTracker>();
            builder.AddScoped<IPublishService, PublishService>();
            builder.AddSingleton<IJobManagement, JobManagement>();
            builder.AddSingleton<ISpecificationFundingStatusService, SpecificationFundingStatusService>();
            builder.AddScoped<ICsvUtils, CsvUtils>();
            builder.AddSingleton<IFileSystemAccess, FileSystemAccess>();
            builder.AddSingleton<IFileSystemCacheSettings, FileSystemCacheSettings>();
            builder.AddSingleton<IPublishedFundingOrganisationGroupingService, PublishedFundingOrganisationGroupingService>();
            builder.AddScoped<IReApplyCustomProfiles, ReApplyCustomProfiles>();
            builder.AddScoped<IPublishedProviderErrorDetection, PublishedProviderErrorDetection>();
            builder.AddScoped<IDetectPublishedProviderErrors, FundingLineValueProfileMismatchErrorDetector>();
            builder.AddScoped<IDetectPublishedProviderErrors, TrustIdMismatchErrorDetector>();

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

            builder
                .AddSingleton<IPublishedProviderVersioningService, PublishedProviderVersioningService>()
                .AddSingleton<IHealthChecker, PublishedProviderVersioningService>();

            builder
                .AddSingleton<IPublishedProviderStatusUpdateService, PublishedProviderStatusUpdateService>()
                .AddSingleton<IHealthChecker, PublishedProviderStatusUpdateService>();

            builder
                .AddSingleton<IPublishedProviderVersionService, PublishedProviderVersionService>()
                .AddSingleton<IHealthChecker, PublishedProviderVersionService>();

            builder
                .AddSingleton<IPublishedSearchService, PublishedSearchService>()
                .AddSingleton<IHealthChecker, PublishedSearchService>();
            builder
                .AddSingleton<IPublishedProviderStatusUpdateSettings>(_ =>
                    {
                        PublishedProviderStatusUpdateSettings settings = new PublishedProviderStatusUpdateSettings();

                        config.Bind("PublishedProviderStatusUpdateSettings", settings);

                        return settings;
                    }
                );

            builder
                .AddSingleton<IPublishingEngineOptions>(_ => new PublishingEngineOptions(config));

            builder
                .AddSingleton<IBlobContainerRepository, BlobContainerRepository>();

            builder
                .AddSingleton<Common.Storage.IBlobClient, CommonBlobClient>((ctx) =>
                {
                    BlobStorageOptions storageSettings = new BlobStorageOptions();

                    config.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "publishedproviderversions";

                    return new CommonBlobClient(storageSettings, ctx.GetService<IBlobContainerRepository>());
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

                return new VersionRepository<PublishedProviderVersion>(resultsRepostory);
            });

            builder.AddSingleton<IVersionRepository<PublishedFundingVersion>, VersionRepository<PublishedFundingVersion>>((ctx) =>
            {
                CosmosDbSettings ProviderSourceDatasetVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", ProviderSourceDatasetVersioningDbSettings);

                ProviderSourceDatasetVersioningDbSettings.ContainerName = "publishedfunding";

                CosmosRepository cosmosRepository = new CosmosRepository(ProviderSourceDatasetVersioningDbSettings);

                return new VersionRepository<PublishedFundingVersion>(cosmosRepository);
            });


            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.ContainerName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                return new CalculationResultsRepository(calcsCosmosRepostory);
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

            builder.AddSingleton<IJobsRunning, JobsRunning>();

            builder.AddSingleton<ICalculationPrerequisiteCheckerService, CalculationPrerequisiteCheckerService>();

            builder.AddSingleton<ICalculationsService, CalculationsService>();

            builder.AddSingleton<IPublishedProviderContentsGeneratorResolver>(ctx =>
            {
                PublishedProviderContentsGeneratorResolver resolver = new PublishedProviderContentsGeneratorResolver();

                resolver.Register("1.0", new Generators.Schema10.PublishedProviderContentsGenerator());
                resolver.Register("1.1", new Generators.Schema11.PublishedProviderContentsGenerator());

                return resolver;
            });

            builder.AddSingleton<IPublishedFundingContentsGeneratorResolver>(ctx =>
            {
                PublishedFundingContentsGeneratorResolver resolver = new PublishedFundingContentsGeneratorResolver();

                resolver.Register("1.0", new Generators.Schema10.PublishedFundingContentsGenerator());
                resolver.Register("1.1", new Generators.Schema11.PublishedFundingContentsGenerator());

                return resolver;
            });

            builder.AddSingleton<IPublishedFundingIdGeneratorResolver>(ctx =>
            {
                PublishedFundingIdGeneratorResolver resolver = new PublishedFundingIdGeneratorResolver();

                IPublishedFundingIdGenerator v10Generator = new Generators.Schema10.PublishedFundingIdGenerator();

                resolver.Register("1.0", v10Generator);
                resolver.Register("1.1", v10Generator);

                return resolver;
            });

            builder.AddSingleton<IJobHelperService, JobHelperService>();

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

            builder.AddSingleton<IJobHelperResiliencePolicies>(publishingResiliencePolicies);

            builder.AddSpecificationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddProvidersInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddJobsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddCalculationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddPoliciesInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddSingleton<ITransactionResiliencePolicies>((ctx) =>
            {
                return new TransactionResiliencePolicies()
                {
                    TransactionPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings)
                };
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
                
                return new PublishedFundingUndoBlobStoreRepository(new CommonBlobClient(settings, 
                        ctx.GetService<IBlobContainerRepository>()),
                    ctx.GetService<IPublishingResiliencePolicies>(),
                    ctx.GetService<ILogger>());
            });
            
            builder.AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>();

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            return builder.BuildServiceProvider();
        }

        private static ResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            ResiliencePolicies resiliencePolicies = new ResiliencePolicies
            {
                CalculationResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedProviderVersionRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                SpecificationsRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedFundingRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                FundingFeedSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                PublishedFundingBlobRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PublishedProviderSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                PublishedIndexSearchResiliencePolicy = PublishedIndexSearchResiliencePolicy.GeneratePublishedIndexSearch(),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy)
            };

            return resiliencePolicies;
        }
    }
}