using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Services.Core.AspNet.Extensions;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.FundingDataZone.IoC;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace CalculateFunding.Api.FundingDataZone
{
    public class Startup
    {
        private const string FundingDataZone = "CalculateFunding.Api.FundingDataZone";
        private const string Title = "Funding Data Zone - Microservice API";
        
        private static readonly string AppConfigConnectionString = Environment.GetEnvironmentVariable("AzureConfiguration:ConnectionString");
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
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
                app.ConfigureSwagger(title: Title);
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

            builder.AddFundingDataZoneServices(Configuration);

            builder.AddSingleton<IHealthChecker, ControllerResolverHealthCheck>();

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, FundingDataZone);
            builder.AddApplicationInsightsServiceName(Configuration, FundingDataZone);
            builder.AddLogging(FundingDataZone);
            builder.AddTelemetry();
            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);
            builder.AddHealthCheckMiddleware();

            if (Configuration.IsSwaggerEnabled())
            {
                builder.ConfigureSwaggerServices(title: Title);
            }
        }
    }
}
