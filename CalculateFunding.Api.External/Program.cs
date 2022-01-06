using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CalculateFunding.Api.External
{
    public class Program
    {
        private static readonly string AppConfigConnectionString = Environment.GetEnvironmentVariable("AzureConfiguration:ConnectionString");

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var startup = new Startup(builder.Configuration);
            builder.Host
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    if (string.IsNullOrWhiteSpace(AppConfigConnectionString))
                    {
                        return;
                    }
                    var settings = config.Build();
                    config.AddAzureAppConfiguration(options =>
                    {
                        options.Connect(AppConfigConnectionString);
                        options.UseFeatureFlags();
                    });
                });
            startup.ConfigureServices(builder.Services);
            var app = builder.Build();
            startup.Configure(app, app.Lifetime, app.Environment, app.Services.GetService<IApiVersionDescriptionProvider>());
            app.Run();
        }
    }
}
