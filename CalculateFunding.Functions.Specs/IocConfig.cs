using System;
using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Functions.Common;
using FluentValidation;
using CalculateFunding.Services.Specs.Validators;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.Validators;

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
            builder.AddScoped<IValidator<PolicyCreateModel>, PolicyCreateModelValidator>();
            builder.AddScoped<IValidator<CalculationCreateModel>, CalculationCreateModelValidator>();
            builder.AddScoped<IValidator<SpecificationCreateModel>, SpecificationCreateModelValidator>();
            builder.AddScoped<IValidator<AssignDefinitionRelationshipMessage>, AssignDefinitionRelationshipMessageValidator>();
            builder.AddScoped<ISpecificationsSearchService, SpecificationsSearchService>();

            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<SpecificationsMappingProfile>());

            builder.AddSingleton(mappingConfig.CreateMapper());

            IConfigurationRoot config = Services.Core.Extensions.ConfigHelper.AddConfig();

            builder.AddCosmosDb(config);

            builder.AddServiceBus(config);

            builder.AddSearch(config);

            builder.AddLogging(config, "CalculateFunding.Functions.Specs");
        }
    }
}
