using System;
using AutoMapper;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Calcs.CodeGen;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Calcs.Validators;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.CodeMetadataGenerator;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Compiler.Languages;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using FluentValidation;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

namespace CalculateFunding.Functions.Calcs
{
    static public class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build(IConfigurationRoot config)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = BuildServiceProvider(config);
            }

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
            {
                _serviceProvider = BuildServiceProvider(message, config);
            }

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
            builder.AddSingleton<ICalculationsRepository, CalculationsRepository>();
            builder.AddSingleton<ICalculationService, CalculationService>();
            builder.AddSingleton<ICalculationsSearchService, CalculationSearchService>();
            builder.AddSingleton<ICalculationCodeReferenceUpdate, CalculationCodeReferenceUpdate>();
            builder.AddSingleton<ITokenChecker, TokenChecker>();
            builder.AddSingleton<IValidator<Calculation>, CalculationModelValidator>();
            builder.AddSingleton<IPreviewService, PreviewService>();
            builder.AddSingleton<ICompilerFactory, CompilerFactory>();
            builder.AddSingleton<IDatasetRepository, DatasetRepository>();
            builder.AddSingleton<IJobService, JobService>();
            builder
                .AddSingleton<CSharpCompiler>()
                .AddSingleton<VisualBasicCompiler>()
                .AddSingleton<VisualBasicSourceFileGenerator>();
            builder.AddSingleton<ISourceFileGeneratorProvider, SourceFileGeneratorProvider>();
            builder.AddSingleton<IValidator<PreviewRequest>, PreviewRequestModelValidator>();
            builder.AddSingleton<ISpecificationRepository, SpecificationRepository>();
            builder.AddSingleton<IPoliciesRepository, PoliciesRepository>();
            builder.AddSingleton<IBuildProjectsService, BuildProjectsService>();
            builder.AddSingleton<IBuildProjectsRepository, BuildProjectsRepository>();
            builder.AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();
            builder.AddSingleton<ICancellationTokenProvider, InactiveCancellationTokenProvider>();
            builder.AddSingleton<ISourceCodeService, SourceCodeService>();
            builder.AddSingleton<IJobHelperService, JobHelperService>();
            builder
               .AddSingleton<IDatasetDefinitionFieldChangesProcessor, DatasetDefinitionFieldChangesProcessor>();

            builder.AddSingleton<ISourceFileRepository, SourceFileRepository>(ctx =>
            {
                BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                config.Bind("CommonStorageSettings", blobStorageOptions);

                blobStorageOptions.ContainerName = "source";

                return new SourceFileRepository(blobStorageOptions);
            });

            builder.AddSingleton<IVersionRepository<CalculationVersion>, VersionRepository<CalculationVersion>>((ctx) =>
            {
                CosmosDbSettings calcsVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calcsVersioningDbSettings);

                calcsVersioningDbSettings.CollectionName = "calcs";

                CosmosRepository resultsRepostory = new CosmosRepository(calcsVersioningDbSettings);

                return new VersionRepository<CalculationVersion>(resultsRepostory);
            });

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddCosmosDb(config, "calcs");
            }
            else
            {
                builder.AddCosmosDb(config);
            }

            builder.AddFeatureToggling(config);

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddResultsInterServiceClient(config);
            builder.AddProvidersInterServiceClient(config);
            builder.AddSpecificationsInterServiceClient(config);
            builder.AddDatasetsInterServiceClient(config);
            builder.AddJobsInterServiceClient(config);
            builder.AddPoliciesInterServiceClient(config);

            builder.AddCaching(config);

            builder.AddEngineSettings(config);

            builder.AddApplicationInsights(config, "CalculateFunding.Functions.Calcs");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Calcs");
            builder.AddLogging("CalculateFunding.Functions.Calcs");
            builder.AddTelemetry();

            PolicySettings policySettings = builder.GetPolicySettings(config);
            ResiliencePolicies resiliencePolicies = CreateResiliencePolicies(policySettings);
            builder.AddSingleton<ICalcsResiliencePolicies>(resiliencePolicies);
            builder.AddSingleton<IJobHelperResiliencePolicies>(resiliencePolicies);
        }

        private static ResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            return new ResiliencePolicies
            {
                CalculationsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                CalculationsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                CacheProviderPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                CalculationsVersionsRepositoryPolicy = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                SpecificationsRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                BuildProjectRepositoryPolicy = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                MessagePolicy = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SourceFilesRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                DatasetsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };
        }
    }
}
