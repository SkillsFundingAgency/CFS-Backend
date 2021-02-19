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
using Newtonsoft.Json;

namespace CalculateFunding.Api.Graph
{
    public class Startup
    {
        private static readonly string AppConfigConnectionString = Environment.GetEnvironmentVariable("AzureConfiguration:ConnectionString");

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
            if (!string.IsNullOrEmpty(AppConfigConnectionString))
            {
                app.UseAzureAppConfiguration();
            }

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
                .AddScoped<IHealthChecker, ControllerResolverHealthCheck>();

            builder
                .AddScoped<IGremlinClientFactory, GremlinClientFactory>();
            builder
                .AddScoped<IPathResultsTransform, PathResultsTransform>();
            builder
                .AddScoped<ICosmosGraphDbSettings>(ctx =>
                {
                    CosmosGraphDbSettings settings = new CosmosGraphDbSettings();
                    
                    Configuration.Bind("CosmosGraphSettings", settings);

                    return settings;
                });

            builder
                .AddScoped<IGraphRepository>(ctx =>
                {
                    IGremlinClientFactory gremlinClientFactory = ctx.GetService<IGremlinClientFactory>();
                    IPathResultsTransform pathResultsTransform = ctx.GetService<IPathResultsTransform>();
                    ICosmosGraphDbSettings settings = ctx.GetService<ICosmosGraphDbSettings>();

                    return new GraphRepository(gremlinClientFactory, pathResultsTransform, settings.DegreeOfParallelism == 0 ? 15 : settings.DegreeOfParallelism);
                });

            builder
                .AddScoped<ISpecificationRepository, SpecificationRepository>();

            builder
                .AddScoped<ICalculationRepository, CalculationRepository>();

            builder
                .AddScoped<IGraphService, GraphService>();

            builder
                .AddScoped<IDatasetRepository, DatasetRepository>();

            builder
                .AddScoped<IFundingLineRepository, FundingLineRepository>();

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
