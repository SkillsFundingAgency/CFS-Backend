using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Functions.Common;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Models.Calcs;
using FluentValidation;
using CalculateFunding.Services.Calcs.Validators;

namespace CalculateFunding.Functions.Calcs
{
    static public class IocConfig
    {
        const string CollectionName = "calcs";

        static public IServiceProvider Build()
        {
            var serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddScoped<ICalculationsRepository, CalculationsRepository>();

            builder
               .AddScoped<ICalculationService, CalculationService>();

            builder
                .AddScoped<IValidator<Calculation>, CalculationModelValidator>();



            IConfigurationRoot config = Services.Core.Extensions.ConfigHelper.AddConfig();

            builder.AddCosmosDb(config, CollectionName);

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddLogging(config, "CalculateFunding.Functions.Calcs");
        }
    }
}
