using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Http;
using CalculateFunding.Common.WebApi.Middleware;
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
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Polly.Bulkhead;

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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            RegisterComponents(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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

            app.UseMiddleware<LoggedInUserMiddleware>();

            app.UseMvc();

            app.UseHealthCheckMiddleware();

            app.MapWhen(
                context => !context.Request.Path.Value.StartsWith("/swagger"),
                appBuilder => appBuilder.UseMiddleware<ApiKeyMiddleware>());

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Calcs Microservice API");
                c.DocumentTitle = "Calcs Microservice - Swagger";
            });
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddScoped<IHealthChecker, ControllerResolverHealthCheck>();

            builder
                .AddSingleton<ICalculationsRepository, CalculationsRepository>();

            builder
               .AddScoped<ICalculationService, CalculationService>()
               .AddSingleton<ICalculationNameInUseCheck, CalculationNameInUseCheck>()
               .AddSingleton<IInstructionAllocationJobCreation, InstructionAllocationJobCreation>()
               .AddScoped<IHealthChecker, CalculationService>()
               .AddScoped<ICreateCalculationService, CreateCalculationService>();

            builder
                .AddSingleton<ICalculationCodeReferenceUpdate, CalculationCodeReferenceUpdate>()
                .AddSingleton<ITokenChecker, TokenChecker>();

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
               .AddSingleton<ISpecificationRepository, SpecificationRepository>();

            builder
                .AddScoped<IBuildProjectsService, BuildProjectsService>()
                .AddScoped<IHealthChecker, BuildProjectsService>();

            builder
                  .AddSingleton<IBuildProjectsRepository, BuildProjectsRepository>()
                  .AddSingleton<IHealthChecker, BuildProjectsRepository>();

            builder
                .AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();

            builder
              .AddSingleton<IDatasetRepository, DatasetRepository>();

            builder.AddSingleton<ISourceCodeService, SourceCodeService>();

            builder
                .AddSingleton<IDatasetDefinitionFieldChangesProcessor, DatasetDefinitionFieldChangesProcessor>();

            builder.AddSingleton<ICalculationEngineRunningChecker, CalculationEngineRunningChecker>();

            builder.AddSingleton<ISourceFileRepository, SourceFileRepository>((ctx) =>
            {
                BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                Configuration.Bind("AzureStorageSettings", blobStorageOptions);

                blobStorageOptions.ContainerName = "source";

                return new SourceFileRepository(blobStorageOptions);
            });

            builder.AddSingleton<IVersionRepository<CalculationVersion>, VersionRepository<CalculationVersion>>((ctx) =>
            {
                CosmosDbSettings calcsVersioningDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", calcsVersioningDbSettings);

                calcsVersioningDbSettings.ContainerName = "calcs";

                CosmosRepository resultsRepostory = new CosmosRepository(calcsVersioningDbSettings);

                return new VersionRepository<CalculationVersion>(resultsRepostory);
            });

            builder
                .AddSingleton<ICancellationTokenProvider, HttpContextCancellationProvider>();

            builder.AddUserProviderFromRequest();

            builder.AddCosmosDb(Configuration);

            builder.AddSearch(Configuration);

            builder.AddServiceBus(Configuration);

            builder.AddResultsInterServiceClient(Configuration);
            builder.AddProvidersInterServiceClient(Configuration);
            builder.AddSpecificationsInterServiceClient(Configuration);
            builder.AddDatasetsInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);
            builder.AddPoliciesInterServiceClient(Configuration);

            builder.AddCaching(Configuration);

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsForApiApp(Configuration, "CalculateFunding.Api.Calcs");
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Calcs");
            builder.AddLogging("CalculateFunding.Api.Calcs");
            builder.AddTelemetry();

            builder.AddEngineSettings(Configuration);

            builder.AddFeatureToggling(Configuration);

            builder.AddPolicySettings(Configuration);

            builder.AddSingleton<ICalcsResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

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
                    PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };
            });

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            builder.AddHealthCheckMiddleware();

            builder.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Calcs Microservice API", Version = "v1" });
                c.AddSecurityDefinition("API Key", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "Ocp-Apim-Subscription-Key",
                    In = ParameterLocation.Header,
                });
            });
        }
    }
}
