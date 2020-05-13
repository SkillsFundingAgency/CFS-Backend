using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
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
using Microsoft.OpenApi.Models;
using Polly.Bulkhead;
using Serilog;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Policy.TemplateBuilder;
using TemplateMetadataSchema10 = CalculateFunding.Common.TemplateMetadata.Schema10;
using TemplateMetadataSchema11 = CalculateFunding.Common.TemplateMetadata.Schema11;

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

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Policy Microservice API", Version = "v1" });
                c.AddSecurityDefinition("API Key", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "Ocp-Apim-Subscription-Key",
                    In = ParameterLocation.Header,
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                   {
                     new OpenApiSecurityScheme
                     {
                       Reference = new OpenApiReference
                       {
                         Type = ReferenceType.SecurityScheme,
                         Id = "API Key"
                       }
                      },
                      new string[] { }
                    }
                });
            });
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

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Policy Microservice API");
                c.DocumentTitle = "Policy Microservice - Swagger";
            });

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
                 CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                 cosmosDbSettings.ContainerName = "policy";

                 Configuration.Bind("CosmosDbSettings", cosmosDbSettings);

                 CosmosRepository cosmosRepostory = new CosmosRepository(cosmosDbSettings);

                 return new PolicyRepository(cosmosRepostory);
             });

            builder.AddSingleton<IPolicyResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                Polly.AsyncPolicy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                return new PolicyResiliencePolicies()
                {
                    PolicyRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProvider = redisPolicy,
                    FundingSchemaRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    FundingTemplateRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };
            });

            builder.AddSingleton<IValidator<FundingConfiguration>, SaveFundingConfigurationValidator>();
            builder.AddSingleton<IValidator<FundingPeriodsJsonModel>, FundingPeriodJsonModelValidator>();

            RegisterTemplateBuilderComponents(builder);

            builder.AddPolicySettings(Configuration);

            MapperConfiguration fundingConfMappingConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<FundingConfigurationMappingProfile>();
            });

            builder
                .AddSingleton(fundingConfMappingConfig.CreateMapper());

            builder.AddCaching(Configuration);
           
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Policy");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Policy");
            builder.AddLogging("CalculateFunding.Api.Policy");
            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            builder.AddHealthCheckMiddleware();
        }

        public void RegisterTemplateBuilderComponents(IServiceCollection builder)
        {
            CosmosDbSettings settings = new CosmosDbSettings();
            Configuration.Bind("CosmosDbSettings", settings);
            settings.ContainerName = "templatebuilder";
            CosmosRepository cosmos = new CosmosRepository(settings);
            builder
                .AddSingleton<ITemplateBuilderService, TemplateBuilderService>()
                .AddSingleton<IHealthChecker, TemplateBuilderService>()
                .AddSingleton<AbstractValidator<TemplateCreateCommand>, TemplateCreateCommandValidator>()
                .AddSingleton<AbstractValidator<TemplateContentUpdateCommand>, TemplateContentUpdateCommandValidator>()
                .AddSingleton<AbstractValidator<TemplateMetadataUpdateCommand>, TemplateMetadataUpdateCommandValidator>()
                .AddSingleton<AbstractValidator<Reference>, AuthorValidator>()
                .AddSingleton<AbstractValidator<FindTemplateVersionQuery>, FindTemplateVersionQueryValidator>()
                .AddSingleton<ITemplateRepository, TemplateRepository>(ctx => new TemplateRepository(cosmos))
                .AddSingleton<ITemplateVersionRepository, TemplateVersionRepository>(ctx => new TemplateVersionRepository(cosmos))
                .AddSingleton<ITemplateMetadataResolver>(ctx =>
                {
                    var resolver = new TemplateMetadataResolver();
                    var logger = ctx.GetService<ILogger>();
                    
                    var schema10Generator = new TemplateMetadataSchema10.TemplateMetadataGenerator(logger);
                    resolver.Register("1.0", schema10Generator);

                    var schema11Generator = new TemplateMetadataSchema11.TemplateMetadataGenerator(logger);
                    resolver.Register("1.1", schema11Generator);

                    return resolver;
                });
        }
    }
}
