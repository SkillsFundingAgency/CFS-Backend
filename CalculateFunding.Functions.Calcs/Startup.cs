using System;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Storage;
using CalculateFunding.Functions.Calcs.ServiceBus;
using CalculateFunding.Models.Calcs;

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
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.DeadletterProcessor;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Bulkhead;

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
            }

            builder.AddScoped<IApplyTemplateCalculationsService, ApplyTemplateCalculationsService>();
            builder.AddSingleton<ICalculationsRepository, CalculationsRepository>();
            builder.AddSingleton<ITemplateContentsCalculationQuery, TemplateContentsCalculationQuery>();
            builder.AddSingleton<IApplyTemplateCalculationsJobTrackerFactory, ApplyTemplateCalculationsJobTrackerFactory>();
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
            builder.AddSingleton<IDatasetRepository, DatasetRepository>();
            builder.AddSingleton<IJobService, JobService>();
            builder
                .AddSingleton<CSharpCompiler>()
                .AddSingleton<VisualBasicCompiler>()
                .AddSingleton<VisualBasicSourceFileGenerator>();
            builder.AddSingleton<ISourceFileGeneratorProvider, SourceFileGeneratorProvider>();
            builder.AddSingleton<IValidator<PreviewRequest>, PreviewRequestModelValidator>();
            builder.AddScoped<IBuildProjectsService, BuildProjectsService>();
            builder.AddSingleton<IBuildProjectsRepository, BuildProjectsRepository>();
            builder.AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();
            builder.AddSingleton<ICancellationTokenProvider, InactiveCancellationTokenProvider>();
            builder.AddSingleton<ISourceCodeService, SourceCodeService>();
            builder.AddScoped<IJobHelperService, JobHelperService>();
            builder.AddScoped<IJobManagement, JobManagement>();

            builder
               .AddScoped<IDatasetDefinitionFieldChangesProcessor, DatasetDefinitionFieldChangesProcessor>();

            builder.AddSingleton<ICalculationEngineRunningChecker, CalculationEngineRunningChecker>();

            builder.AddScoped<IValidator<CalculationCreateModel>, CalculationCreateModelValidator>();

            builder
              .AddScoped<IValidator<CalculationEditModel>, CalculationEditModelValidator>();

            builder.AddSingleton<ISourceFileRepository, SourceFileRepository>(ctx =>
            {
                BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                config.Bind("AzureStorageSettings", blobStorageOptions);

                blobStorageOptions.ContainerName = "source";

                return new SourceFileRepository(blobStorageOptions);
            });

            builder.AddSingleton<IVersionRepository<CalculationVersion>, VersionRepository<CalculationVersion>>((ctx) =>
            {
                CosmosDbSettings calcsVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calcsVersioningDbSettings);

                calcsVersioningDbSettings.ContainerName = "calcs";

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
            builder
                .AddSingleton<ISearchRepository<CalculationIndex>, SearchRepository<CalculationIndex>>();
            builder
                .AddSingleton<ISearchRepository<ProviderCalculationResultsIndex>, SearchRepository<ProviderCalculationResultsIndex>>();

            builder.AddServiceBus(config, "calcs");

            builder.AddProvidersInterServiceClient(config);
            builder.AddSpecificationsInterServiceClient(config);
            builder.AddDatasetsInterServiceClient(config);
            builder.AddJobsInterServiceClient(config);
            builder.AddPoliciesInterServiceClient(config);

            builder.AddCaching(config);

            builder.AddEngineSettings(config);

            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Calcs");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Calcs");
            builder.AddLogging("CalculateFunding.Functions.Calcs");
            builder.AddTelemetry();

            PolicySettings policySettings = builder.GetPolicySettings(config);
            BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            ResiliencePolicies resiliencePolicies = CreateResiliencePolicies(totalNetworkRequestsPolicy);

            builder.AddSingleton<ICalcsResiliencePolicies>(resiliencePolicies);
            builder.AddSingleton<IJobHelperResiliencePolicies>(resiliencePolicies);
            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = resiliencePolicies.JobsApiClient,
                };

            });

            return builder.BuildServiceProvider();
        }

        private static ResiliencePolicies CreateResiliencePolicies(Policy totalNetworkRequestsPolicy)
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
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SourceFilesRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                DatasetsRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };
        }
    }
}
