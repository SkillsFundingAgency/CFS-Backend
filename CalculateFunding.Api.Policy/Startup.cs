using System.Threading;
using AutoMapper;
using CacheCow.Server.Core.Mvc;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Results;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.MappingProfiles;
using CalculateFunding.Services.Policy.Validators;
using CalculateFunding.Services.Providers.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly.Bulkhead;
using Serilog;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Policy.TemplateBuilder;
using TemplateMetadataSchema10 = CalculateFunding.Common.TemplateMetadata.Schema10;
using TemplateMetadataSchema11 = CalculateFunding.Common.TemplateMetadata.Schema11;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Policy.Caching.Http;

namespace CalculateFunding.Api.Policy
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.ConfigureSwagger(title: "Policy Microservice API");

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
            builder.AddHttpCachingMvc();
            builder.AddQueryProviderAndExtractorForViewModelMvc<FundingStructure, TemplateMetadataContentsTimedETagProvider, TemplateMatadataContentsTimedETagExtractor>(false);
            
            builder.AddSingleton<IFundingStructureService, FundingStructureService>()
                .AddSingleton<IValidator<UpdateFundingStructureLastModifiedRequest>, UpdateFundingStructureLastModifiedRequestValidator>()
                .AddSpecificationsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan)
                .AddCalculationsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan)
                .AddResultsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan);
            
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddSingleton<IIoCValidatorFactory, ValidatorFactory>()
                .AddSingleton<IValidator<Reference>, AuthorValidator>();
            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder
                .AddSingleton<IFundingStreamService, FundingStreamService>()
                .AddSingleton<IHealthChecker, FundingStreamService>()
                .AddSingleton<IValidator<FundingStreamSaveModel>, FundingStreamSaveModelValidator>();

            builder
                .AddSingleton<IFundingPeriodService, FundingPeriodService>()
                .AddSingleton<IHealthChecker, FundingPeriodService>()
                .AddSingleton<IFundingPeriodValidator, FundingPeriodValidator>();

            builder
                .AddSingleton<IFundingSchemaService, FundingSchemaService>()
                .AddSingleton<IHealthChecker, FundingSchemaService>();

            builder
                .AddSingleton<IFundingConfigurationService, FundingConfigurationService>()
                .AddSingleton<IHealthChecker, FundingConfigurationService>();

            builder
                .AddSingleton<IFundingTemplateService, FundingTemplateService>()
                .AddSingleton<IHealthChecker, FundingTemplateService>();

            builder
                .AddSingleton<IFundingTemplateValidationService, FundingTemplateValidationService>()
                .AddSingleton<IHealthChecker, FundingTemplateValidationService>();

            builder
                .AddSingleton<IFundingDateService, FundingDateService>()
                .AddSingleton<IHealthChecker, FundingDateService>();

            builder
                .AddSingleton<IBlobContainerRepository, BlobContainerRepository>();

            builder
                .AddSingleton<IFundingSchemaRepository, FundingSchemaRepository>((ctx) =>
                {
                    BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                    Configuration.Bind("AzureStorageSettings", blobStorageOptions);

                    blobStorageOptions.ContainerName = "fundingschemas";

                    return new FundingSchemaRepository(blobStorageOptions, ctx.GetService<IBlobContainerRepository>());
                });

            builder
               .AddSingleton<IFundingTemplateRepository, FundingTemplateRepository>((ctx) =>
               {
                   BlobStorageOptions blobStorageOptions = new BlobStorageOptions();

                   Configuration.Bind("AzureStorageSettings", blobStorageOptions);

                   blobStorageOptions.ContainerName = "fundingtemplates";

                   return new FundingTemplateRepository(blobStorageOptions, ctx.GetService<IBlobContainerRepository>());
               });

            builder
             .AddSingleton<IPolicyRepository, PolicyRepository>((ctx) =>
             {
                 CosmosDbSettings cosmosDbSettings = new CosmosDbSettings
                 {
                     ContainerName = "policy"
                 };

                 Configuration.Bind("CosmosDbSettings", cosmosDbSettings);

                 CosmosRepository cosmosRepository = new CosmosRepository(cosmosDbSettings);

                 return new PolicyRepository(cosmosRepository);
             });

            builder.AddSingleton<IPolicyResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                Polly.AsyncPolicy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                return new PolicyResiliencePolicies
                {
                    PolicyRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProvider = redisPolicy,
                    FundingSchemaRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    FundingTemplateRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    TemplatesSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    TemplatesRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    ResultsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddSingleton<IValidator<FundingConfiguration>, SaveFundingConfigurationValidator>();
            builder.AddSingleton<IValidator<FundingPeriodsJsonModel>, FundingPeriodJsonModelValidator>();
            builder.AddSingleton<IValidator<FundingDate>, SaveFundingDateValidator>();

            RegisterTemplateBuilderComponents(builder);

            builder.AddPolicySettings(Configuration);
            builder.AddJobsInterServiceClient(Configuration);

            MapperConfiguration fundingConfMappingConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<FundingConfigurationMappingProfile>();
            });

            builder
                .AddSingleton(fundingConfMappingConfig.CreateMapper());

            builder.AddSearch(Configuration);

            builder.AddSingleton<TemplateSearchService>()
                .AddSingleton<IHealthChecker, TemplateSearchService>();

            builder
                .AddSingleton<ISearchRepository<TemplateIndex>, SearchRepository<TemplateIndex>>();

            builder.AddCaching(Configuration);
           
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Policy");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Policy");
            builder.AddLogging("CalculateFunding.Api.Policy");
            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();           
            builder.AddHealthCheckMiddleware();

            builder.ConfigureSwaggerServices(title: "Policy Microservice API");
        }

        public void RegisterTemplateBuilderComponents(IServiceCollection builder)
        {
            CosmosDbSettings settings = new CosmosDbSettings();
            Configuration.Bind("CosmosDbSettings", settings);
            settings.ContainerName = "templatebuilder";
            CosmosRepository cosmos = new CosmosRepository(settings);
            builder
                .AddSingleton<ITemplateBuilderService, TemplateBuilderService>()
                .AddSingleton<ITemplateBlobService, TemplateBlobService>()
                .AddSingleton<IHealthChecker, TemplateBuilderService>()
                .AddSingleton<AbstractValidator<TemplateCreateCommand>, TemplateCreateCommandValidator>()
                .AddSingleton<AbstractValidator<TemplateCreateAsCloneCommand>, TemplateCreateAsCloneCommandValidator>()
                .AddSingleton<AbstractValidator<TemplateFundingLinesUpdateCommand>, TemplateContentUpdateCommandValidator>()
                .AddSingleton<AbstractValidator<TemplateDescriptionUpdateCommand>, TemplateDescriptionUpdateCommandValidator>()
                .AddSingleton<AbstractValidator<TemplatePublishCommand>, TemplatePublishCommandValidator>()
                .AddSingleton<AbstractValidator<Reference>, AuthorValidator>()
                .AddSingleton<AbstractValidator<FindTemplateVersionQuery>, FindTemplateVersionQueryValidator>()
                .AddSingleton<ITemplateRepository, TemplateRepository>(ctx => new TemplateRepository(cosmos))
                .AddSingleton<ITemplateVersionRepository, TemplateVersionRepository>(ctx => new TemplateVersionRepository(cosmos))
                .AddSingleton<ITemplateMetadataResolver>(ctx =>
                {
                    TemplateMetadataResolver resolver = new TemplateMetadataResolver();
                    ILogger logger = ctx.GetService<ILogger>();
                    
                    TemplateMetadataSchema10.TemplateMetadataGenerator schema10Generator = new TemplateMetadataSchema10.TemplateMetadataGenerator(logger);
                    resolver.Register("1.0", schema10Generator);

                    TemplateMetadataSchema11.TemplateMetadataGenerator schema11Generator = new TemplateMetadataSchema11.TemplateMetadataGenerator(logger);
                    resolver.Register("1.1", schema11Generator);

                    return resolver;
                });
        }
    }
}
