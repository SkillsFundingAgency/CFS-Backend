using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System;

namespace CalculateFunding.Api.Datasets
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
                    });
                });
            startup.ConfigureServices(builder.Services);
            var app = builder.Build();
            startup.Configure(app, app.Environment);
            app.Run();
        }
    }
}
