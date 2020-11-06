using System;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Functions.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Notifications;
using CalculateFunding.Services.Notifications.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
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
            RegisterComponents(builder.Services, builder.GetFunctionsConfigurationToIncludeHostJson());
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfiguration azureFuncConfig = null)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig(azureFuncConfig);

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder
                .AddSingleton<OnNotificationEventTrigger>();
            }

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
                .AddSingleton<INotificationService, NotificationService>();

            builder.AddFeatureToggling(config);

            builder.AddServiceBus(config, "notifications");

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Notifications");
            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Notifications");
            builder.AddLogging("CalculateFunding.Functions.Notifications");
            builder.AddTelemetry();

            builder.AddPolicySettings(config);

            builder.AddSingleton<INotificationsResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new NotificationsResiliencePolicies
                {
                    MessagePolicy = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy),
                };
            });

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            return builder.BuildServiceProvider();
        }
    }
}
