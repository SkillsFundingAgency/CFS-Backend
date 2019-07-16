using System;
using AutoMapper;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Calcs.CodeGen;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Calcs.MappingProfiles;
using CalculateFunding.Services.Calcs.Validators;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.CodeMetadataGenerator;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Compiler.Languages;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using FluentValidation;
using Polly.Bulkhead;
using CalculateFunding.Functions.Calcs.ServiceBus;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Calcs.Startup))]

namespace CalculateFunding.Functions.Calcs
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<CalcsAddRelationshipToBuildProject>();
            builder.AddSingleton<OnCalcsCreateDraftEvent>();
            builder.AddSingleton<OnCalcsInstructAllocationResultsFailure>();
            builder.AddSingleton<OnCalcsInstructAllocationResults>();
            builder.AddSingleton<OnCalculationAggregationsJobCompleted>();
            builder.AddSingleton<OnDataDefinitionChanges>();
            builder.AddSingleton<OnEditCalculationSpecificationEvent>();
            builder.AddSingleton<OnEditSpecificationEvent>();
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

            MapperConfiguration resultsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<PolicyMappingProfile>();
            });

            builder
                .AddSingleton(resultsConfig.CreateMapper());

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

            return builder.BuildServiceProvider();
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
                DatasetsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };
        }
    }
}
