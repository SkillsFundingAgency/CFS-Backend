using System;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Datasets
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
            builder
                .AddScoped<IDefinitionsService, DefinitionsService>();

            builder
                .AddScoped<IDataSetsRepository, DataSetsRepository>();

            IConfigurationRoot config = ConfigHelper.AddConfig();

            builder.AddCosmosDb(config);
            
            builder.AddLogging(config, "CalculateFunding.Functions.Datasets");
        }
    }
}
