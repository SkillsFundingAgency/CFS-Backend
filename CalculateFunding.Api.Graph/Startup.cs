using CalculateFunding.Common.Graph;
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Neo4j.Driver;

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
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.ConfigureSwagger(title: "Graph Microservice API");

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
                .AddScoped<IGraphRepository, GraphRepository>();

            builder
                .AddScoped<ISpecificationRepository, SpecificationRepository>();

            builder
                .AddScoped<ICalculationRepository, CalculationRepository>();

            builder
                .AddScoped<IGraphService, GraphService>();

            builder
                .AddScoped<IDatasetRepository, DatasetRepository>();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();
           
            builder.AddHealthCheckMiddleware();
            
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Graph");
            builder.AddApplicationInsightsServiceName(Configuration, "CalculateFunding.Api.Graph");
            builder.AddLogging("CalculateFunding.Api.Graph");

            builder.ConfigureSwaggerServices(title: "Graph Microservice API");
        }
    }
}
