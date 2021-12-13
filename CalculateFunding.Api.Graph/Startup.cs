using CalculateFunding.Common.Graph.Cosmos;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using CalculateFunding.Services.Core.Helpers;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;
using CalculateFunding.Services.Core.Options;
using Polly.Bulkhead;

namespace CalculateFunding.Api.Graph
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // services.AddControllers()
            //    .AddNewtonsoftJson(_ => 
            //        _.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto);
            
            services.AddControllers()
                .AddNewtonsoftJson();

            RegisterComponents(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            if (Configuration.IsSwaggerEnabled())
            {
                app.ConfigureSwagger(title: "Graph Microservice API");
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
            builder.AddAppConfiguration();

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();
            builder
                .AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder
                .AddSingleton<IGremlinClientFactory, GremlinClientFactory>();
            builder
                .AddSingleton<IPathResultsTransform, PathResultsTransform>();
            builder
                .AddSingleton<ICosmosGraphDbSettings>(ctx =>
                {
                    CosmosGraphDbSettings settings = new CosmosGraphDbSettings();
                    
                    Configuration.Bind("CosmosGraphSettings", settings);

                    return settings;
                });


            builder.AddCaching(Configuration);

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(Configuration);
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);
            GraphResiliencePolicies resiliencePolicies = new GraphResiliencePolicies
            {
                CacheProviderPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy)
            };

            builder.AddSingleton<IGraphResiliencePolicies>(resiliencePolicies);

            builder
                .AddSingleton<IGraphRepository>(ctx =>
                {
                    IGremlinClientFactory gremlinClientFactory = ctx.GetService<IGremlinClientFactory>();
                    IPathResultsTransform pathResultsTransform = ctx.GetService<IPathResultsTransform>();
                    ICosmosGraphDbSettings settings = ctx.GetService<ICosmosGraphDbSettings>();

                    return new GraphRepository(gremlinClientFactory, pathResultsTransform, settings.DegreeOfParallelism == 0 ? 15 : settings.DegreeOfParallelism);
                });

            builder
                .AddSingleton<ISpecificationRepository, SpecificationRepository>();

            builder
                .AddSingleton<ICalculationRepository, CalculationRepository>();

            builder
                .AddSingleton<IGraphService, GraphService>();

            builder
                .AddSingleton<IDatasetRepository, DatasetRepository>();

            builder
                .AddSingleton<IFundingLineRepository, FundingLineRepository>();

            builder
                .AddSingleton<IEnumRepository, EnumRepository>();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();
           
            builder.AddHealthCheckMiddleware();

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Graph");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Graph");
            builder.AddLogging("CalculateFunding.Api.Graph");

            if (Configuration.IsSwaggerEnabled())
            {
                builder.ConfigureSwaggerServices(title: "Graph Microservice API");
            }
        }
    }
}
