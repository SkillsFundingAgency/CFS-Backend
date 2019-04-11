using System;
using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.AzureStorage;
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
using CalculateFunding.Services.Datasets.Validators;
using CalculateFunding.Services.Providers;
using CalculateFunding.Services.Providers.Interfaces;
using FluentValidation;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using Polly;
using Polly.Bulkhead;

namespace CalculateFunding.Functions.Datasets
{
    static public class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build(IConfigurationRoot config)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = BuildServiceProvider(config);
            }

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider(IConfigurationRoot config)
        {
            var serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        public static IServiceProvider Build(Message message, IConfigurationRoot config)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = BuildServiceProvider(message, config);
            }

            IUserProfileProvider userProfileProvider = _serviceProvider.GetService<IUserProfileProvider>();

            Reference user = message.GetUserDetails();

            userProfileProvider.SetUser(user.Id, user.Name);

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider(Message message, IConfigurationRoot config)
        {
            var serviceProvider = new ServiceCollection();

            serviceProvider.AddUserProviderFromMessage(message);

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            builder
                .AddSingleton<IDefinitionsService, DefinitionsService>();

            builder
                .AddSingleton<IDatasetService, DatasetService>();

            builder
                .AddSingleton<IProcessDatasetService, ProcessDatasetService>();

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

                    config.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "datasets";

                    return new BlobClient(storageSettings);
                });

            builder.AddSingleton<IProvidersResultsRepository, ProvidersResultsRepository>((ctx) =>
            {
                CosmosDbSettings dbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", dbSettings);

                dbSettings.CollectionName = "providerdatasets";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(dbSettings);

                return new ProvidersResultsRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<IDatasetRepository, DataSetsRepository>();

            builder.AddSingleton<IDatasetSearchService, DatasetSearchService>();

            builder.AddSingleton<IDatasetDefinitionSearchService, DatasetDefinitionSearchService>();

            builder
               .AddSingleton<IDefinitionSpecificationRelationshipService, DefinitionSpecificationRelationshipService>();

            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>();

            builder
               .AddSingleton<IExcelDatasetReader, ExcelDatasetReader>();

            builder.AddSingleton<IProviderService, ProviderService>();

            builder
               .AddSingleton<ICalcsRepository, CalcsRepository>();

            builder
                .AddSingleton<IResultsRepository, ResultsRepository>();

            builder.AddTransient<IValidator<DatasetUploadValidationModel>, DatasetItemValidator>();

            MapperConfiguration dataSetsConfig = new MapperConfiguration(c => c.AddProfile<DatasetsMappingProfile>());
            builder
                .AddSingleton(dataSetsConfig.CreateMapper());

            builder.AddSingleton<IVersionRepository<ProviderSourceDatasetVersion>, VersionRepository<ProviderSourceDatasetVersion>>((ctx) =>
            {
                CosmosDbSettings ProviderSourceDatasetVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", ProviderSourceDatasetVersioningDbSettings);

                ProviderSourceDatasetVersioningDbSettings.CollectionName = "providerdatasets";

                CosmosRepository cosmosRepository = new CosmosRepository(ProviderSourceDatasetVersioningDbSettings);

                return new VersionRepository<ProviderSourceDatasetVersion>(cosmosRepository);
            });

            builder.AddSingleton<IDatasetsAggregationsRepository, DatasetsAggregationsRepository>((ctx) =>
            {
                CosmosDbSettings dbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", dbSettings);

                dbSettings.CollectionName = "datasetaggregations";

                CosmosRepository aggsCosmosRepostory = new CosmosRepository(dbSettings);

                return new DatasetsAggregationsRepository(aggsCosmosRepostory);
            });

            builder.AddCalcsInterServiceClient(config);
            builder.AddResultsInterServiceClient(config);
            builder.AddSpecificationsInterServiceClient(config);
            builder.AddJobsInterServiceClient(config);

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddCosmosDb(config, "datasets");
            }
            else
            {
                builder.AddCosmosDb(config);
            }

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsights(config, "CalculateFunding.Functions.Datasets");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Datasets");
            builder.AddLogging("CalculateFunding.Functions.Datasets");
            builder.AddTelemetry();

            builder.AddPolicySettings(config);

            builder.AddFeatureToggling(config);

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
                    BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });
        }
    }
}
