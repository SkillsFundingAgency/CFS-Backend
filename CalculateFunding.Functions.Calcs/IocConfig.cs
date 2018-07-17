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
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Models;

namespace CalculateFunding.Functions.Calcs
{
    static public class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build(IConfigurationRoot config)
        {
            if (_serviceProvider == null)
                _serviceProvider = BuildServiceProvider(config);

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider(IConfigurationRoot config)
        {
            var serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        public static IServiceProvider Build(Message message, IConfigurationRoot config)
        {
            if (_serviceProvider == null)
                _serviceProvider = BuildServiceProvider(message, config);

            IUserProfileProvider userProfileProvider = _serviceProvider.GetService<IUserProfileProvider>();

            Reference user = message.GetUserDetails();

            userProfileProvider.SetUser(user.Id, user.Name);

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider(Message message, IConfigurationRoot config)
        {
            var serviceProvider = new ServiceCollection();

            serviceProvider.AddUserProviderFromMessage(message);

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            builder
                .AddSingleton<Services.Calcs.Interfaces.ICalculationsRepository, Services.Calcs.CalculationsRepository>();

            builder
               .AddSingleton<ICalculationService, CalculationService>();

            builder
               .AddSingleton<ICalculationsSearchService, CalculationSearchService>();

            builder
                .AddSingleton<IValidator<Calculation>, CalculationModelValidator>();

            builder
               .AddSingleton<IBuildProjectsRepository, BuildProjectsRepository>();

            builder
                .AddSingleton<IPreviewService, PreviewService>();

            builder
               .AddSingleton<ICompilerFactory, CompilerFactory>();

            builder
                .AddSingleton<CSharpCompiler>()
                .AddSingleton<VisualBasicCompiler>()
                .AddSingleton<VisualBasicSourceFileGenerator>();

            builder
              .AddSingleton<ISourceFileGeneratorProvider, SourceFileGeneratorProvider>();

            builder
               .AddSingleton<IValidator<PreviewRequest>, PreviewRequestModelValidator>();

            builder
                .AddSingleton<Services.Calcs.Interfaces.IProviderResultsRepository, Services.Calcs.ProviderResultsRepository>();

            builder
               .AddSingleton<ISpecificationRepository, SpecificationRepository>();

            builder
                .AddSingleton<IBuildProjectsService, BuildProjectsService>();

            builder
                .AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddCosmosDb(config, "calcs");
            }
            else
            {
                builder.AddCosmosDb(config);
            }

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddResultsInterServiceClient(config);
            builder.AddSpecificationsInterServiceClient(config);

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
                    CalculationsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                };
            });
        }
    }
}
