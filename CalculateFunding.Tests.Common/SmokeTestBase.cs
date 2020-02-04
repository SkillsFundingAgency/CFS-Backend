using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using CalculateFunding.Service.Core.Extensions;

namespace CalculateFunding.Tests.Common
{
    public class SmokeTestBase
    {
        protected static bool _isDevelopment;
        private static string _entityPathBase;
        private static TimeSpan _timeout;
        protected static IServiceCollection _services;

        public static void SetupTests(string serviceName)
        {
            Guard.IsNullOrWhiteSpace(serviceName, nameof(serviceName));

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("local.settings.json")
                .Build();

            _services = new ServiceCollection();

            _services.AddSingleton(configuration);

            _isDevelopment = configuration["ASPNETCORE_ENVIRONMENT"] == "Development";

            _services.AddSingleton((ctx) =>
            {
                return AddServiceBus(configuration, serviceName);
            });

            _timeout = TimeSpan.FromSeconds(_isDevelopment ? 10 : 30);

            if (_isDevelopment)
            {
                _entityPathBase = "{1}-{0}";
            }
            else
            {
                _entityPathBase = "{0}/Subscriptions/{1}";
            }
        }

        public async Task<(IEnumerable<SmokeResponse> responses, string uniqueId)> RunSmokeTest(string functionName,
            string queueName,
            Action<Message> action,
            string topicName = null)
        {
            ServiceProvider serviceProvider = _services.BuildServiceProvider();

            if (_isDevelopment)
            {
                using (AzureStorageEmulatorAutomation azureStorageEmulatorAutomation = new AzureStorageEmulatorAutomation())
                {
                    await azureStorageEmulatorAutomation.Init();
                    await azureStorageEmulatorAutomation.Start();

                    return await RunSmokeTest(serviceProvider.GetRequiredService<IMessengerService>(),
                    functionName,
                    queueName,
                    action,
                    topicName);

                }
            }
            else
            {
                return await RunSmokeTest(serviceProvider.GetRequiredService<IMessengerService>(),
                    functionName,
                    queueName,
                    action,
                    topicName);
            }
        }

        private async Task<(IEnumerable<SmokeResponse> responses, string uniqueId)> RunSmokeTest(IMessengerService messengerService,
            string functionName,
            string queueName,
            Action<Message> action,
            string topicName)
        {
            Guard.IsNullOrWhiteSpace(functionName, nameof(functionName));
            Guard.IsNullOrWhiteSpace(queueName, nameof(queueName));
            Guard.ArgumentNotNull(action, nameof(action));

            string uniqueId = Guid.NewGuid().ToString();

            IDictionary<string, string> properties = new Dictionary<string, string> { { "smoketest", uniqueId } };

            if (!_isDevelopment && topicName != null)
            {
                await messengerService.SendToTopic(topicName,
                    uniqueId.ToString(),
                    properties);
            }
            else
            {
                await messengerService.SendToQueue(queueName,
                    uniqueId.ToString(),
                    properties);
            }

            if (_isDevelopment)
            {
                IEnumerable<string> smokeResponsesFromFunction = await messengerService.ReceiveMessages<string>(queueName,
                    _timeout);

                Message message = new Message();
                message.UserProperties.Add("smoketest", smokeResponsesFromFunction?.FirstOrDefault(_ => _ == uniqueId));

                action(message);
            }

            return (await messengerService.ReceiveMessages<SmokeResponse>(string.Format(_entityPathBase, 
                ServiceBusConstants.TopicNames.SmokeTest, 
                functionName), 
                _timeout), 
                uniqueId);
        }

        private static IMessengerService AddServiceBus(IConfiguration configuration, string serviceName)
        {
            if (_isDevelopment)
            {
                return new QueueMessengerService("UseDevelopmentStorage=true", serviceName);
            }
            else
            {
                ServiceBusSettings serviceBusSettings = new ServiceBusSettings();

                configuration.Bind("ServiceBusSettings", serviceBusSettings);

                return new MessengerService(serviceBusSettings, serviceName);
            }
        }
    }
}
