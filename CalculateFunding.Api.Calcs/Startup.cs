using AutoMapper;
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
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Http;
using CalculateFunding.Common.WebApi.Middleware;
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
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Compiler.Languages;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Polly;
using Polly.Bulkhead;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;

namespace CalculateFunding.Api.Calcs
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson();

            RegisterComponents(services);

            services.AddFeatureManagement();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            if (Configuration.IsSwaggerEnabled())
            {
                app.ConfigureSwagger(title: "Calcs Microservice API");
            }

            app.MapWhen(
                    context => !context.Request.Path.Value.StartsWith("/swagger"),
                    appBuilder => {
                        appBuilder.UseMiddleware<ApiKeyMiddleware>();
                        appBuilder.UseHealthCheckMiddleware();
                        appBuilder.UseMiddleware<LoggedInUserMiddleware>();
                        appBuilder.UseRouting();
                        appBuilder.UseAuthentication();
                        appBuilder.UseAuthorization();
                        appBuilder.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder.AddScoped<ICodeContextCache, CodeContextCache>()
                .AddScoped<ICodeContextBuilder, CodeContextBuilder>();
            
            builder.AddSingleton<ICalculationFundingLineQueryService, CalculationFundingLineQueryService>();
            
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddSingleton(Configuration);
            builder
                .AddScoped<IHealthChecker, ControllerResolverHealthCheck>();

            builder
                .AddSingleton<ICalculationsRepository, CalculationsRepository>((ctx) =>
                {
                    CosmosDbSettings calcsVersioningDbSettings = new CosmosDbSettings();

                    Configuration.Bind("CosmosDbSettings", calcsVersioningDbSettings);

                    calcsVersioningDbSettings.ContainerName = "calcs";

                    CosmosRepository resultsRepostory = new CosmosRepository(calcsVersioningDbSettings);

                    return new CalculationsRepository(resultsRepostory);
                });

            builder
               .AddScoped<ICalculationService, CalculationService>()
               .AddSingleton<ICalculationNameInUseCheck, CalculationNameInUseCheck>()
               .AddScoped<IInstructionAllocationJobCreation, InstructionAllocationJobCreation>()
               .AddScoped<IHealthChecker, CalculationService>()
               .AddScoped<ICreateCalculationService, CreateCalculationService>();

            builder
                .AddScoped<IQueueReIndexSpecificationCalculationRelationships, QueueReIndexSpecificationCalculationRelationships>();

            builder
                .AddSingleton<ICalculationCodeReferenceUpdate, CalculationCodeReferenceUpdate>();

            builder
                .AddSingleton<ICalculationsSearchService, CalculationSearchService>()
                .AddSingleton<IHealthChecker, CalculationSearchService>();

            builder
                .AddSingleton<IValidator<Calculation>, CalculationModelValidator>();

            builder
               .AddScoped<IValidator<CalculationCreateModel>, CalculationCreateModelValidator>();

            builder
               .AddScoped<IValidator<CalculationEditModel>, CalculationEditModelValidator>();

            builder
                .AddScoped<IPreviewService, PreviewService>()
                .AddScoped<IHealthChecker, PreviewService>();

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
                .AddScoped<IBuildProjectsService, BuildProjectsService>()
                .AddScoped<IHealthChecker, BuildProjectsService>();

            builder
                .AddScoped<IDatasetReferenceService, DatasetReferenceService>();

            builder
                .AddSingleton<IBuildProjectsRepository, BuildProjectsRepository>((ctx) =>
                {
                    CosmosDbSettings calcsVersioningDbSettings = new CosmosDbSettings();

                    Configuration.Bind("CosmosDbSettings", calcsVersioningDbSettings);

                    calcsVersioningDbSettings.ContainerName = "calcs";

                    CosmosRepository resultsRepostory = new CosmosRepository(calcsVersioningDbSettings);

                    return new BuildProjectsRepository(resultsRepostory);
                });

            builder
                .AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();

            builder.AddSingleton<ISourceCodeService, SourceCodeService>();

            builder
                .AddSingleton<IDatasetDefinitionFieldChangesProcessor, DatasetDefinitionFieldChangesProcessor>();

            builder.AddScoped<ICalculationEngineRunningChecker, CalculationEngineRunningChecker>();
            
            builder
                .AddScoped<IApproveAllCalculationsJobAction, ApproveAllCalculationsJobAction>();

            builder.AddSingleton<ISourceFileRepository, SourceFileRepository>((ctx) =>
            {
                BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", blobStorageOptions);

                blobStorageOptions.ContainerName = "source";

                IBlobContainerRepository blobContainerRepository = new BlobContainerRepository(blobStorageOptions);
                return new SourceFileRepository(blobContainerRepository);
            });

            builder
                .AddSingleton<IVersionRepository<CalculationVersion>, VersionRepository<CalculationVersion>>((ctx) =>
            {
                CosmosDbSettings calcsVersioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", calcsVersioningDbSettings);

                calcsVersioningDbSettings.ContainerName = "calcs";

                CosmosRepository resultsRepostory = new CosmosRepository(calcsVersioningDbSettings);

                return new VersionRepository<CalculationVersion>(resultsRepostory, new NewVersionBuilderFactory<CalculationVersion>());
            });

            builder
                .AddSingleton<ICancellationTokenProvider, HttpContextCancellationProvider>();           

            MapperConfiguration calcConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<CalculationsMappingProfile>();               
            });

            builder
                .AddSingleton(calcConfig.CreateMapper());

            builder.AddSearch(Configuration);
            builder
                .AddSingleton<ISearchRepository<CalculationIndex>, SearchRepository<CalculationIndex>>();
            builder
                .AddSingleton<ISearchRepository<ProviderCalculationResultsIndex>, SearchRepository<ProviderCalculationResultsIndex>>();

            builder.AddServiceBus(Configuration);

            builder.AddScoped<IJobManagement, JobManagement>();
            builder.AddScoped<ICalculationsFeatureFlag, CalculationsFeatureFlag>();
            builder.AddScoped<IGraphRepository, GraphRepository>();

            builder.AddProvidersInterServiceClient(Configuration);
            builder.AddSpecificationsInterServiceClient(Configuration);
            builder.AddDatasetsInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);
            builder.AddGraphInterServiceClient(Configuration);
            builder.AddPoliciesInterServiceClient(Configuration);
            builder.AddResultsInterServiceClient(Configuration);
            builder.AddCalcEngineInterServiceClient(Configuration);

            builder.AddCaching(Configuration);

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Calcs");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Calcs");

            builder.AddLogging("CalculateFunding.Api.Calcs");
            builder.AddTelemetry();
            builder.AddEngineSettings(Configuration);

            builder.AddFeatureToggling(Configuration);

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(Configuration);
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            ResiliencePolicies resiliencePolicies = CreateResiliencePolicies(totalNetworkRequestsPolicy);

            builder.AddSingleton<ICalcsResiliencePolicies>(resiliencePolicies);
            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = resiliencePolicies.JobsApiClient,
                };

            });

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            builder.AddHealthCheckMiddleware();

            if (Configuration.IsSwaggerEnabled())
            {
                builder.ConfigureSwaggerServices(title: "Calcs Microservice API", version: "v1");
            }
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
                SourceFilesRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                DatasetsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                GraphApiClientPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ResultsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CalcEngineApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
            };
        }
    }
}
