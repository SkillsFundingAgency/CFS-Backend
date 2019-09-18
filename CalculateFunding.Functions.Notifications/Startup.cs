using System;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Notifications;
using CalculateFunding.Services.Notifications.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Notifications.Startup))]

namespace CalculateFunding.Functions.Notifications
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
            builder
                .AddSingleton<OnNotificationEventTrigger>();

            builder
                .AddSingleton<INotificationService, NotificationService>();

            builder.AddFeatureToggling(config);

            builder.AddServiceBus(config);

            builder.AddApplicationInsightsForFunctionApps(config, "CalculateFunding.Functions.Notifications");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Notifications");
            builder.AddLogging("CalculateFunding.Functions.Notifications");
            builder.AddTelemetry();

            builder.AddPolicySettings(config);

            builder.AddSingleton<INotificationsResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new NotificationsResiliencePolicies
                {
                    MessagePolicy = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy),
                };
            });

            return builder.BuildServiceProvider();
        }
    }
}
