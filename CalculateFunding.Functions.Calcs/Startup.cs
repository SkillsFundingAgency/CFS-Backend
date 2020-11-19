using System;
using System.Threading;
using AutoMapper;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.Config.ApiClient.CalcEngine;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.Common.Config.ApiClient.Graph;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Results;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Storage;
using CalculateFunding.Functions.Calcs.ServiceBus;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Calcs.Analysis;
using CalculateFunding.Services.Calcs.Caching;
using CalculateFunding.Services.Calcs.CodeGen;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Calcs.MappingProfiles;
using CalculateFunding.Services.Calcs.Validators;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.CodeMetadataGenerator;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Analysis;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Compiler.Languages;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Functions.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.DeadletterProcessor;
using CalculateFunding.Services.Processing.Interfaces;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Polly;
using Polly.Bulkhead;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Calcs.Startup))]

namespace CalculateFunding.Functions.Calcs
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services, builder.GetFunctionsConfigurationToIncludeHostJson());
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfiguration azureFuncConfig = null)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig(azureFuncConfig);

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddFeatureManagement();

            builder.AddSingleton<IConfiguration>(ctx => config);

            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<CalcsAddRelationshipToBuildProject>();
                builder.AddScoped<OnCalcsInstructAllocationResultsFailure>();
                builder.AddScoped<OnCalcsInstructAllocationResults>();
                builder.AddScoped<OnCalculationAggregationsJobCompleted>();
                builder.AddScoped<OnDataDefinitionChanges>();
                builder.AddScoped<OnApplyTemplateCalculations>();
                builder.AddScoped<OnApplyTemplateCalculationsFailure>();
                builder.AddScoped<OnReIndexSpecificationCalculationRelationships>();
                builder.AddScoped<OnReIndexSpecificationCalculationRelationshipsFailure>();
                builder.AddScoped<OnDeleteCalculations>();
                builder.AddScoped<OnDeleteCalculationsFailure>();
                builder.AddScoped<OnUpdateCodeContextCache>();
                builder.AddScoped<OnUpdateCodeContextCacheFailure>();
                builder.AddScoped<OnApproveAllCalculations>();
                builder.AddScoped<OnApproveAllCalculationsFailure>();
            }

            builder.AddScoped<IApplyTemplateCalculationsService, ApplyTemplateCalculationsService>();
            builder.AddSingleton<ICalculationsRepository, CalculationsRepository>((ctx) =>
            {
                CosmosDbSettings calcsVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calcsVersioningDbSettings);

                calcsVersioningDbSettings.ContainerName = "calcs";

                CosmosRepository resultsRepostory = new CosmosRepository(calcsVersioningDbSettings);

                return new CalculationsRepository(resultsRepostory);
            });

            builder.AddScoped<ICalculationService, CalculationService>()
                .AddScoped<IInstructionAllocationJobCreation, InstructionAllocationJobCreation>()
                .AddScoped<ICreateCalculationService, CreateCalculationService>();
            builder.AddSingleton<ICalculationNameInUseCheck, CalculationNameInUseCheck>();
            builder.AddSingleton<ICalculationsSearchService, CalculationSearchService>();
            builder.AddSingleton<ICalculationCodeReferenceUpdate, CalculationCodeReferenceUpdate>();
            builder.AddSingleton<ITokenChecker, TokenChecker>();
            builder.AddSingleton<IValidator<Calculation>, CalculationModelValidator>();
            builder.AddScoped<IPreviewService, PreviewService>();
            builder.AddSingleton<ICompilerFactory, CompilerFactory>();
            //builder.AddSingleton<IDatasetRepository, DatasetRepository>();
            builder.AddScoped<IJobService, JobService>();
            builder.AddScoped<IApproveAllCalculationsJobAction, ApproveAllCalculationsJobAction>();
            builder
                .AddSingleton<CSharpCompiler>()
                .AddSingleton<VisualBasicCompiler>()
                .AddSingleton<VisualBasicSourceFileGenerator>();
            builder.AddSingleton<ISourceFileGeneratorProvider, SourceFileGeneratorProvider>();
            builder.AddSingleton<IValidator<PreviewRequest>, PreviewRequestModelValidator>();
            builder.AddScoped<IBuildProjectsService, BuildProjectsService>();
            builder.AddSingleton<IBuildProjectsRepository, BuildProjectsRepository>((ctx) =>
            {
                CosmosDbSettings calcsVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calcsVersioningDbSettings);

                calcsVersioningDbSettings.ContainerName = "calcs";

                CosmosRepository resultsRepostory = new CosmosRepository(calcsVersioningDbSettings);

                return new BuildProjectsRepository(resultsRepostory);
            });

            builder.AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();
            builder.AddSingleton<ICancellationTokenProvider, InactiveCancellationTokenProvider>();
            builder.AddSingleton<ISourceCodeService, SourceCodeService>();
            builder.AddScoped<IDeadletterService, DeadletterService>();
            builder.AddScoped<IJobManagement, JobManagement>();

            MapperConfiguration calculationsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<CalculationsMappingProfile>();
            });

            builder
                .AddSingleton(calculationsConfig.CreateMapper());

            builder.AddScoped<IReIndexGraphRepository, ReIndexGraphRepository>();
            builder.AddScoped<ISpecificationCalculationAnalysis, SpecificationCalculationAnalysis>();
            builder.AddScoped<IReIndexSpecificationCalculationRelationships, ReIndexSpecificationCalculationRelationships>();
            builder.AddScoped<ICalculationAnalysis, CalculationAnalysis>();

            builder
               .AddScoped<IDatasetDefinitionFieldChangesProcessor, DatasetDefinitionFieldChangesProcessor>();

            builder.AddScoped<ICalculationEngineRunningChecker, CalculationEngineRunningChecker>();

            builder.AddScoped<IValidator<CalculationCreateModel>, CalculationCreateModelValidator>();

            builder.AddScoped<IApproveAllCalculationsService, ApproveAllCalculationsService>();

            builder
               .AddScoped<IDatasetReferenceService, DatasetReferenceService>();

            builder
              .AddScoped<IValidator<CalculationEditModel>, CalculationEditModelValidator>();
            
            builder.AddSingleton<ISourceFileRepository, SourceFileRepository>(ctx =>
            {
                BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                config.Bind("AzureStorageSettings", blobStorageOptions);

                blobStorageOptions.ContainerName = "source";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(blobStorageOptions);
                return new SourceFileRepository(blobContainerRepository);
            });

            builder.AddSingleton<IVersionRepository<CalculationVersion>, VersionRepository<CalculationVersion>>((ctx) =>
            {
                CosmosDbSettings calcsVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calcsVersioningDbSettings);

                calcsVersioningDbSettings.ContainerName = "calcs";

                CosmosRepository resultsRepostory = new CosmosRepository(calcsVersioningDbSettings);

                return new VersionRepository<CalculationVersion>(resultsRepostory);
            });

            builder.AddFeatureToggling(config);

            builder.AddSearch(config);
            builder
                .AddSingleton<ISearchRepository<CalculationIndex>, SearchRepository<CalculationIndex>>();
            builder
                .AddSingleton<ISearchRepository<ProviderCalculationResultsIndex>, SearchRepository<ProviderCalculationResultsIndex>>();

            builder.AddServiceBus(config, "calcs");
            builder.AddScoped<ICalculationsFeatureFlag, CalculationsFeatureFlag>();
            builder.AddScoped<IGraphRepository, GraphRepository>();

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            builder.AddProvidersInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddSpecificationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddDatasetsInterServiceClient(config);
            builder.AddJobsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddGraphInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddPoliciesInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddResultsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddCalcEngineInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddCaching(config);

            builder.AddEngineSettings(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Calcs");
            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Calcs");
            builder.AddLogging("CalculateFunding.Functions.Calcs");
            builder.AddTelemetry();

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(config);
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            ResiliencePolicies resiliencePolicies = CreateResiliencePolicies(totalNetworkRequestsPolicy);

            builder.AddSingleton<ICalcsResiliencePolicies>(resiliencePolicies);
            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) => new JobManagementResiliencePolicies()
            {
                JobsApiClient = resiliencePolicies.JobsApiClient,
            });

            builder.AddScoped<ICodeContextCache, CodeContextCache>()
                .AddScoped<ICodeContextBuilder, CodeContextBuilder>();

            return builder.BuildServiceProvider();
        }

        private static ResiliencePolicies CreateResiliencePolicies(AsyncPolicy totalNetworkRequestsPolicy)
        {
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
                ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SourceFilesRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                DatasetsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                GraphApiClientPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ResultsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CalcEngineApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
            };
        }
    }
}
