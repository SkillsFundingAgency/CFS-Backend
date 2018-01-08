using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AutoMapper;
using CalculateFunding.Functions.Common.Extensions;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.Extensions.Configuration;
using CalculateFunding.Functions.Common;

namespace CalculateFunding.Functions.Specs
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
            builder.AddScoped<ISpecificationsRepository, SpecificationsRepository>();
            builder.AddScoped<ISpecificationsService, SpecificationsService>();

            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<SpecificationsMappingProfile>());

            builder.AddSingleton(mappingConfig.CreateMapper());

            builder.AddSingleton(new LoggerFactory()
                   .AddConsole()
                   .AddSerilog()
                    .AddDebug())
                .AddLogging();

            IConfigurationRoot config = ConfigHelper.AddConfig();

            builder.AddCosmosDb(config);
        }
    }
}
