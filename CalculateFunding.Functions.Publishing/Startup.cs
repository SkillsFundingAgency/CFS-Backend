using System;
using CalculateFunding.Functions.Publishing.ServiceBus;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Publishing.Startup))]

namespace CalculateFunding.Functions.Publishing
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<OnRefreshFunding>();
            builder.AddSingleton<OnApproveFunding>();
            builder.AddSingleton<OnPublishFunding>();
            builder.AddSingleton<IRefreshService, RefreshService>();
            builder.AddSingleton<IApproveService, ApproveService>();
            builder.AddSingleton<IPublishService, PublishService>();
            builder.AddApplicationInsights(config, "CalculateFunding.Functions.Publishing");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Publishing");

            builder.AddLogging("CalculateFunding.Functions.Publishing");

            builder.AddTelemetry();

            return builder.BuildServiceProvider();
        }
    }
}
