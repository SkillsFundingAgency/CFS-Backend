using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Models.Calcs;
using FluentValidation;
using CalculateFunding.Services.Calcs.Validators;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Languages;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Calcs.CodeGen;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.CodeMetadataGenerator;
using CalculateFunding.Repositories.Common.Cosmos;

namespace CalculateFunding.Functions.Calcs
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
            builder
                .AddScoped<ICalculationsRepository, CalculationsRepository>();

            builder
               .AddScoped<ICalculationService, CalculationService>();

            builder
               .AddScoped<ICalculationsSearchService, CalculationSearchService>();

            builder
                .AddScoped<IValidator<Calculation>, CalculationModelValidator>();

            builder
               .AddScoped<IBuildProjectsRepository, BuildProjectsRepository>();

            builder
                .AddScoped<IPreviewService, PreviewService>();

            builder
               .AddScoped<ICompilerFactory, CompilerFactory>();

            builder
                .AddScoped<CSharpCompiler>()
                .AddScoped<VisualBasicCompiler>()
                .AddScoped<VisualBasicSourceFileGenerator>();

            builder
              .AddScoped<ISourceFileGeneratorProvider, SourceFileGeneratorProvider>();

            builder
               .AddScoped<IValidator<PreviewRequest>, PreviewRequestModelValidator>();

            builder
                .AddScoped<ICalculationEngine, CalculationEngine>();

            builder
             .AddScoped<IAllocationFactory, AllocationFactory>();

            builder
                .AddSingleton<IProviderResultsRepository, ProviderResultsRepository>();

            builder
               .AddScoped<ISpecificationRepository, SpecificationRepository>();

            builder
                .AddScoped<IBuildProjectsService, BuildProjectsService>();

            builder
               .AddSingleton<IExcelDatasetReader, ExcelDatasetReader>();

            builder
                .AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();

            IConfigurationRoot config = Services.Core.Extensions.ConfigHelper.AddConfig();

            builder.AddSingleton<IProviderSourceDatasetsRepository, ProviderSourceDatasetsRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.CollectionName = "results";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                return new ProviderSourceDatasetsRepository(calcsCosmosRepostory);
            });

            builder.AddCosmosDb(config);

            builder.AddSearch(config);

            builder.AddEventHub(config);

            builder.AddInterServiceClient(config);

            builder.AddCaching(config);

            builder.AddLogging(config, "CalculateFunding.Functions.Calcs");
        }
    }
}
