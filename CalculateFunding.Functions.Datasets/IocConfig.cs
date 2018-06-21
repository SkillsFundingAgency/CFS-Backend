using System;
using AutoMapper;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.AzureStorage;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Datasets
{
    static public class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build()
        {
            if (_serviceProvider == null)
                _serviceProvider = BuildServiceProvider();

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider()
        {
            var serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            builder
                .AddScoped<IDefinitionsService, DefinitionsService>();

            builder
                .AddScoped<IDatasetService, DatasetService>();

            builder
              .AddScoped<IValidator<CreateNewDatasetModel>, CreateNewDatasetModelValidator>();

            builder
                .AddScoped<IValidator<DatasetVersionUpdateModel>, DatasetVersionUpdateModelValidator>();

            builder
              .AddScoped<IValidator<DatasetMetadataModel>, DatasetMetadataModelValidator>();

            builder
                .AddScoped<IValidator<GetDatasetBlobModel>, GetDatasetBlobModelValidator>();

            builder
               .AddScoped<IValidator<CreateDefinitionSpecificationRelationshipModel>, CreateDefinitionSpecificationRelationshipModelValidator>();

            builder
                .AddScoped<IBlobClient, BlobClient>((ctx) =>
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

                dbSettings.CollectionName = "providersourcedatasets";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(dbSettings);

                ICacheProvider cacheProvider = ctx.GetService<ICacheProvider>();

                return new ProvidersResultsRepository(calcsCosmosRepostory, cacheProvider);
            });

            builder.AddScoped<IDatasetRepository, DataSetsRepository>();

            builder.AddScoped<IDatasetSearchService, DatasetSearchService>();

            builder
               .AddScoped<IDefinitionSpecificationRelationshipService, DefinitionSpecificationRelationshipService>();

            builder
                .AddScoped<ISpecificationsRepository, SpecificationsRepository>();

            builder
               .AddSingleton<IExcelDatasetReader, ExcelDatasetReader>();

            builder
                .AddScoped<IProviderRepository, ProviderRepository>();

            builder
               .AddScoped<ICalcsRepository, CalcsRepository>();

            MapperConfiguration dataSetsConfig = new MapperConfiguration(c => c.AddProfile<DatasetsMappingProfile>());
            builder
                .AddSingleton(dataSetsConfig.CreateMapper());

            builder.AddInterServiceClient(config);

            builder.AddCosmosDb(config);

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config);
            builder.AddLogging("CalculateFunding.Functions.Datasets");
            builder.AddTelemetry();
        }
    }
}
