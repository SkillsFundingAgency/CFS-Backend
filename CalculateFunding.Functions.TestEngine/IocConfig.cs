using System;
using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.CodeMetadataGenerator;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.TestRunner;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Repositories;
using CalculateFunding.Services.TestRunner.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.TestEngine
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
                .AddScoped<IBuildProjectRepository, BuildProjectRepository>();

            builder
                .AddScoped<IGherkinParserService, GherkinParserService>();

            builder
               .AddScoped<IGherkinParser, GherkinParser>();

            builder
                .AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();

            builder
              .AddSingleton<IProviderRepository, ProviderRepository>();

            builder
                .AddScoped<IStepParserFactory, StepParserFactory>();

            builder
                .AddSingleton<ITestResultsRepository, TestResultsRepository>();

            builder
                .AddSingleton<ISpecificationRepository, SpecificationRepository>();

            builder
                .AddSingleton<IScenariosRepository, ScenariosRepository>();

            builder
                .AddScoped<ITestEngineService, Services.TestRunner.Services.TestEngineService>();

            builder
                .AddScoped<ITestEngine, Services.TestRunner.TestEngine>();

            builder
               .AddScoped<IGherkinExecutor, GherkinExecutor>();

            builder.AddSingleton<Services.TestRunner.Interfaces.IProviderRepository, Services.TestRunner.Services.ProviderRepository>((ctx) =>
            {
                CosmosDbSettings providersDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", providersDbSettings);

                providersDbSettings.CollectionName = "results";

                CosmosRepository providersCosmosRepostory = new CosmosRepository(providersDbSettings);

                ICacheProvider cacheProvider = ctx.GetService<ICacheProvider>();

                return new Services.TestRunner.Services.ProviderRepository(providersCosmosRepostory, cacheProvider);
            });

            MapperConfiguration resultsMappingConfiguration = new MapperConfiguration(c => c.AddProfile<ResultsMappingProfile>());
            builder
                .AddSingleton(resultsMappingConfiguration.CreateMapper());

            builder.AddSingleton<ITestResultsService, TestResultsService>();

            builder.AddCosmosDb(config);

            builder.AddSearch(config);

            builder.AddInterServiceClient(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config);

            builder.AddLogging("CalculateFunding.Functions.TestRunner");

            builder.AddTelemetry();
        }
    }
}
