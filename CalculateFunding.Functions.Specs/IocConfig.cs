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

            builder.AddScoped<ISpecificationsRepository, SpecificationsRepository>();
            builder.AddScoped<ISpecificationsService, SpecificationsService>();
            builder.AddScoped<IValidator<PolicyCreateModel>, PolicyCreateModelValidator>();
            builder.AddScoped<IValidator<PolicyEditModel>, PolicyEditModelValidator>();
            builder.AddScoped<IValidator<CalculationCreateModel>, CalculationCreateModelValidator>();
            builder.AddScoped<IValidator<SpecificationCreateModel>, SpecificationCreateModelValidator>();
            builder.AddScoped<IValidator<CalculationEditModel>, CalculationEditModelValidator>();
            builder.AddScoped<IValidator<SpecificationEditModel>, SpecificationEditModelValidator>();
            builder.AddScoped<IValidator<AssignDefinitionRelationshipMessage>, AssignDefinitionRelationshipMessageValidator>();
            builder.AddScoped<ISpecificationsSearchService, SpecificationsSearchService>();
            builder.AddSingleton<IResultsRepository, ResultsRepository>();

            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<SpecificationsMappingProfile>());

            builder.AddSingleton(mappingConfig.CreateMapper());

            builder.AddCosmosDb(config);

            builder.AddServiceBus(config);

            builder.AddSearch(config);

            builder.AddCaching(config);

            builder.AddInterServiceClient(config);

            builder.AddPolicySettings(config);

            builder.AddApplicationInsightsTelemetryClient(config);
            builder.AddLogging("CalculateFunding.Functions.Specs");
            builder.AddTelemetry();
        }
    }
}
