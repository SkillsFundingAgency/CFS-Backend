using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Graph Microservice API");
                c.DocumentTitle = "Graph Microservice - Swagger";
            });
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder
                .AddScoped<IHealthChecker, ControllerResolverHealthCheck>();

            builder
                .AddSingleton<IGraphRepository, GraphRepository>(ctx =>
                {
                    GraphDbSettings graphDbSettings = new GraphDbSettings();

                    Configuration.Bind("GraphDbSettings", graphDbSettings);

                    return new GraphRepository(graphDbSettings);
                });

            builder
                .AddSingleton<ICalculationRepository, CalculationRepository>();

            builder
                .AddSingleton<IGraphService, GraphService>();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();

            builder.AddHealthCheckMiddleware();

            builder.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Graph Microservice API", Version = "v1" });
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
