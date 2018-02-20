using System;
using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Results
{
    static public class IocConfig
    {
        static public IServiceProvider Build()
        {
            var serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            //builder
            //    .AddScoped<IDefinitionsService, DefinitionsService>();

            //builder
            //    .AddScoped<IDatasetService, DatasetService>();

            //builder
            //  .AddScoped<IValidator<CreateNewDatasetModel>, CreateNewDatasetModelValidator>();

            //builder
            //  .AddScoped<IValidator<DatasetMetadataModel>, DatasetMetadataModelValidator>();

            //builder
            //    .AddScoped<IValidator<GetDatasetBlobModel>, GetDatasetBlobModelValidator>();

            //builder
            //   .AddScoped<IValidator<CreateDefinitionSpecificationRelationshipModel>, CreateDefinitionSpecificationRelationshipModelValidator>();

            //builder
            //    .AddScoped<IBlobClient, BlobClient>((ctx) =>
            //    {
            //        AzureStorageSettings storageSettings = new AzureStorageSettings();

            //        config.Bind("AzureStorageSettings", storageSettings);

            //        storageSettings.ContainerName = "datasets";

            //        return new BlobClient(storageSettings);
            //    });

            builder.AddScoped<IResultsRepository, ResultsRepository>((ctx) =>
            {
                CosmosDbSettings datasetsDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", datasetsDbSettings);

                datasetsDbSettings.CollectionName = "datasets";

                CosmosRepository datasetsCosmosRepostory = new CosmosRepository(datasetsDbSettings);

                return new ResultsRepository(datasetsCosmosRepostory);
            });

            builder.AddScoped<IResultsSearchService, ResultsSearchService>();

            MapperConfiguration resultsConfig = new MapperConfiguration(c => c.AddProfile<DatasetsMappingProfile>());
            builder
                .AddSingleton(resultsConfig.CreateMapper());

            builder.AddInterServiceClient(config);

            builder.AddCosmosDb(config);

            builder.AddSearch(config);
            
            builder.AddLogging(config, "CalculateFunding.Functions.Results");
        }
    }
}
