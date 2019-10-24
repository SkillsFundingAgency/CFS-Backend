﻿using System;
using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Functions.Datasets.ServiceBus;
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
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.MappingProfiles;
using CalculateFunding.Services.Datasets.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using Polly;
using Polly.Bulkhead;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Datasets.Startup))]

namespace CalculateFunding.Functions.Datasets
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

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            builder
              .AddSingleton<OnDataDefinitionChanges>();

            builder
              .AddSingleton<OnDatasetEvent>();

            builder
              .AddSingleton<OnDatasetValidationEvent>();

            builder
               .AddSingleton<IDefinitionsService, DefinitionsService>();

            builder
                .AddSingleton<IDatasetService, DatasetService>();

            builder
                .AddSingleton<IJobManagement, JobManagement>();

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
               .AddSingleton<IExcelDatasetReader, ExcelDatasetReader>();

            builder
               .AddSingleton<ICalcsRepository, CalcsRepository>();

            builder.AddTransient<IValidator<DatasetUploadValidationModel>, DatasetItemValidator>();

            MapperConfiguration dataSetsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
                c.AddProfile<ProviderMappingProfile>();
            });

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

            builder.AddCalculationsInterServiceClient(config);
            builder.AddResultsInterServiceClient(config);
            builder.AddSpecificationsInterServiceClient(config);
            builder.AddJobsInterServiceClient(config);
            builder.AddProvidersInterServiceClient(config);

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

            builder.AddApplicationInsightsForFunctionApps(config, "CalculateFunding.Functions.Datasets");
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
                    SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProviderRepository = redisPolicy,
                    ProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ProviderRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    DatasetRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    DatasetSearchService = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    DatasetDefinitionSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    BlobClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });


            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };

            });

            return builder.BuildServiceProvider();
        }
    }
}
