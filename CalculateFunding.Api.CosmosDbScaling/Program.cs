using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.API.CosmosDbScaling
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
