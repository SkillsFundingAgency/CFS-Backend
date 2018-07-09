using System;
using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using CalculateFunding.Services.Specs.Validators;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.Validators;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Models;

namespace CalculateFunding.Functions.Specs
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

            builder.AddSingleton<ISpecificationsRepository, SpecificationsRepository>();
            builder.AddSingleton<ISpecificationsService, SpecificationsService>();
            builder.AddSingleton<IValidator<PolicyCreateModel>, PolicyCreateModelValidator>();
            builder.AddSingleton<IValidator<PolicyEditModel>, PolicyEditModelValidator>();
            builder.AddSingleton<IValidator<CalculationCreateModel>, CalculationCreateModelValidator>();
            builder.AddSingleton<IValidator<SpecificationCreateModel>, SpecificationCreateModelValidator>();
            builder.AddSingleton<IValidator<CalculationEditModel>, CalculationEditModelValidator>();
            builder.AddSingleton<IValidator<SpecificationEditModel>, SpecificationEditModelValidator>();
            builder.AddSingleton<IValidator<AssignDefinitionRelationshipMessage>, AssignDefinitionRelationshipMessageValidator>();
            builder.AddSingleton<ISpecificationsSearchService, SpecificationsSearchService>();
            builder.AddSingleton<IResultsRepository, ResultsRepository>();

            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<SpecificationsMappingProfile>());

            builder.AddSingleton(mappingConfig.CreateMapper());

            builder.AddCosmosDb(config);

            builder.AddServiceBus(config);

            builder.AddSearch(config);

            builder.AddCaching(config);

            builder.AddResultsInterServiceClient(config);

            builder.AddPolicySettings(config);

            builder.AddApplicationInsightsTelemetryClient(config);
            builder.AddLogging("CalculateFunding.Functions.Specs");
            builder.AddTelemetry();
        }
    }
}
