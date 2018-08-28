using CalculateFunding.Api.Common.Extensions;
using CalculateFunding.Api.Common.Middleware;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Cosmos;
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
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

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

	        app.UseMiddleware<ApiKeyMiddleware>();

			app.UseMvc();

            app.UseHealthCheckMiddleware();
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddSingleton<ICalculationsRepository, CalculationsRepository>();

            builder
               .AddSingleton<ICalculationService, CalculationService>()
               .AddSingleton<IHealthChecker, CalculationService>();

            builder
               .AddSingleton<ICalculationsSearchService, CalculationSearchService>()
               .AddSingleton<IHealthChecker, CalculationSearchService>();

            builder
                .AddSingleton<IValidator<Calculation>, CalculationModelValidator>();

            builder
               .AddSingleton<IBuildProjectsRepository, BuildProjectsRepository>()
               .AddSingleton<IHealthChecker, BuildProjectsRepository>();

            builder
                .AddSingleton<IPreviewService, PreviewService>()
                .AddSingleton<IHealthChecker, PreviewService>();

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
                .AddSingleton<IProviderResultsRepository, ProviderResultsRepository>()
                .AddSingleton<IHealthChecker, ProviderResultsRepository>();

            builder
               .AddSingleton<ISpecificationRepository, SpecificationRepository>();

            builder
                .AddSingleton<IBuildProjectsService, BuildProjectsService>()
                .AddSingleton<IHealthChecker, BuildProjectsService>();

            builder
                .AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();

            builder.AddUserProviderFromRequest();

            builder.AddCosmosDb(Configuration);

            builder.AddSearch(Configuration);

            builder.AddServiceBus(Configuration);

            builder.AddResultsInterServiceClient(Configuration);
            builder.AddSpecificationsInterServiceClient(Configuration);

            builder.AddCaching(Configuration);

            builder.AddApplicationInsightsTelemetryClient(Configuration);
            builder.AddLogging("CalculateFunding.Api.Calcs");
            builder.AddTelemetry();

            builder.AddPolicySettings(Configuration);

            builder.AddSingleton<ICalcsResilliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ResiliencePolicies
                {
                    CalculationsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                };
            });

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            builder.AddHealthCheckMiddleware();
        }
    }
}
