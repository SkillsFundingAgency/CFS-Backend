using System;
using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
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

            builder.AddScoped<IResultsRepository, ResultsRepository>((ctx) =>
            {
                CosmosDbSettings resultsDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", resultsDbSettings);

                resultsDbSettings.CollectionName = "results";

                CosmosRepository resultsCosmosRepostory = new CosmosRepository(resultsDbSettings);

                return new ResultsRepository(resultsCosmosRepostory);
            });

            builder.AddScoped<IResultsService, ResultsService>();
	        builder.AddScoped<IResultsSearchService, ResultsSearchService>();
			MapperConfiguration resultsConfig = new MapperConfiguration(c => c.AddProfile<DatasetsMappingProfile>());
            builder
                .AddSingleton(resultsConfig.CreateMapper());

            builder.AddInterServiceClient(config);

            builder.AddCosmosDb(config);

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddLogging(config, "CalculateFunding.Functions.Results");
        }
    }
}
