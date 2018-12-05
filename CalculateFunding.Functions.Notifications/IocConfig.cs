using System;
using CalculateFunding.Models;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Notifications;
using CalculateFunding.Services.Notifications.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

namespace CalculateFunding.Functions.Notifications
{
    public static class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build(IConfigurationRoot config)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = BuildServiceProvider(config);
            }

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider(IConfigurationRoot config)
        {
            var serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        public static IServiceProvider Build(Message message, IConfigurationRoot config)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = BuildServiceProvider(message, config);
            }

            IUserProfileProvider userProfileProvider = _serviceProvider.GetService<IUserProfileProvider>();

            Reference user = message.GetUserDetails();

            userProfileProvider.SetUser(user.Id, user.Name);

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider(Message message, IConfigurationRoot config)
        {
            var serviceProvider = new ServiceCollection();

            serviceProvider.AddUserProviderFromMessage(message);

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            builder
                .AddSingleton<INotificationService, NotificationService>();

            builder.AddFeatureToggling(config);

            builder.AddServiceBus(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Notifications");
            builder.AddLogging("CalculateFunding.Functions.Notifications");
            builder.AddTelemetry();

            builder.AddPolicySettings(config);

            builder.AddSingleton<INotificationsResilliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new NotificationsResilliencePolicies
                {
                    MessagePolicy = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy),
                };
            });
        }
    }
}
