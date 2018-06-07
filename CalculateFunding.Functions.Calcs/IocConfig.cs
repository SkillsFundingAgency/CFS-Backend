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
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.CodeMetadataGenerator;
using CalculateFunding.Services.Core.Options;
using Polly.Bulkhead;
using CalculateFunding.Services.Core.Helpers;
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
                .AddScoped<Services.Calcs.Interfaces.ICalculationsRepository, Services.Calcs.CalculationsRepository>();

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
                .AddSingleton<Services.Calcs.Interfaces.IProviderResultsRepository, Services.Calcs.ProviderResultsRepository>();

            builder
               .AddScoped<ISpecificationRepository, SpecificationRepository>();

            builder
                .AddScoped<IBuildProjectsService, BuildProjectsService>();

            builder
                .AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();

            IConfigurationRoot config = Services.Core.Extensions.ConfigHelper.AddConfig();

            builder.AddCosmosDb(config);

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddInterServiceClient(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config);
            builder.AddLogging("CalculateFunding.Functions.Calcs");
            builder.AddTelemetry();

            builder.AddPolicySettings(config);

            builder.AddSingleton<ICalcsResilliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ResiliencePolicies
                {
                    CalculationsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy)
                    
                };
            });
        }
    }
}
