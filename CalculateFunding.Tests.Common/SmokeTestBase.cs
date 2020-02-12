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
using NSubstitute;

namespace CalculateFunding.Tests.Common
{
    public class SmokeTestBase
    {
        protected static bool _isDevelopment;
        protected static bool _useMocking;
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

            _useMocking = configuration["USE_MOCKING"] == "true";

            _services.AddSingleton((ctx) =>
            {
                if (_useMocking)
                {
                    return Substitute.For<IMessengerService, IServiceBusService, IQueueService>(); ;
                }
                else
                {
                    return AddServiceBus(configuration, serviceName);
                }
            });

            _timeout = TimeSpan.FromSeconds(_isDevelopment ? 10 : 30);
        }

        public async Task<(IEnumerable<SmokeResponse> responses, string uniqueId)> RunSmokeTest(string queueName,
            Action<Message> action,
            string topicName = null)
        {
            ServiceProvider serviceProvider = _services.BuildServiceProvider();

            if (_isDevelopment && !_useMocking)
            {
                using (AzureStorageEmulatorAutomation azureStorageEmulatorAutomation = new AzureStorageEmulatorAutomation())
                {
                    await azureStorageEmulatorAutomation.Init();
                    await azureStorageEmulatorAutomation.Start();

                    return await RunSmokeTest(serviceProvider.GetRequiredService<IMessengerService>(),
                    queueName,
                    action,
                    topicName);
                }
            }
            else
            {
                return await RunSmokeTest(serviceProvider.GetRequiredService<IMessengerService>(),
                    queueName,
                    action,
                    topicName);
            }
        }

        private async Task<(IEnumerable<SmokeResponse> responses, string uniqueId)> RunSmokeTest(IMessengerService messengerService,
            string queueName,
            Action<Message> action,
            string topicName)
        {
            Guard.IsNullOrWhiteSpace(queueName, nameof(queueName));
            Guard.ArgumentNotNull(action, nameof(action));

            string uniqueId = Guid.NewGuid().ToString();

            IDictionary<string, string> properties = new Dictionary<string, string> { { "smoketest", uniqueId } };

            string entityPathBase = !_isDevelopment ? $"{ServiceBusConstants.TopicNames.SmokeTest}/Subscriptions/{uniqueId}" : uniqueId;

            if (_useMocking)
            {
                MockReceiveMessages(messengerService, uniqueId, entityPathBase, queueName);
            }

            try
            {
                if(!_isDevelopment)
                {
                    await ((IServiceBusService)messengerService).CreateSubscription("smoketest", uniqueId);
                }

                if (!_isDevelopment && topicName != null)
                {
                    await messengerService.SendToTopic(topicName,
                        uniqueId,
                        properties);
                }
                else
                {
                    await messengerService.SendToQueue(queueName,
                        uniqueId,
                        properties);
                }

                if (_isDevelopment)
                {
                    IEnumerable<string> smokeResponsesFromFunction = await messengerService.ReceiveMessages<string>(queueName,
                        _timeout);

                    Message message = new Message();
                    message.UserProperties.Add("smoketest", smokeResponsesFromFunction?.FirstOrDefault(_ => _ == uniqueId));

                    action = _useMocking ? (msg) => 
                    { 
                        msg.UserProperties["smoketest"].Equals(uniqueId); 
                    } : action;

                    action(message);
                }

                return (await messengerService.ReceiveMessages<SmokeResponse>(entityPathBase,
                    _timeout),
                    uniqueId);
            }
            finally
            {
                if (!_isDevelopment)
                {
                    await ((IServiceBusService)messengerService).DeleteSubscription("smoketest", uniqueId);
                }
                else
                {
                    await ((IQueueService)messengerService).DeleteQueue(uniqueId);
                }

                if (_useMocking)
                {
                    CheckServiceBusCalls(messengerService, uniqueId, queueName, topicName, entityPathBase);
                }
            }
        }

        private static void MockReceiveMessages(IMessengerService messengerService, string uniqueId, string entityPathBase, string queueName)
        {
            messengerService.ReceiveMessages<string>(queueName,
                        _timeout)
                .Returns(new string[] { uniqueId });

            messengerService.ReceiveMessages<SmokeResponse>(entityPathBase,
                    _timeout)
                .Returns(new SmokeResponse[] { new SmokeResponse { InvocationId = uniqueId } });
        }

        private static void CheckServiceBusCalls(IMessengerService messengerService, string uniqueId, string queueName, string topicName, string entityPathBase)
        {
            if (!_isDevelopment && topicName != null)
            {
                messengerService
                    .Received(1)
                    .SendToTopic(topicName,
                    uniqueId,
                    Arg.Any<Dictionary<string, string>>());
            }
            else
            {
                messengerService
                    .Received(1)
                    .SendToQueue(queueName,
                    uniqueId,
                    Arg.Any<Dictionary<string, string>>());
            }

            if (!_isDevelopment)
            {
                ((IServiceBusService)messengerService)
                .Received(1)
                .CreateSubscription("smoketest", uniqueId);

                ((IServiceBusService)messengerService)
                    .Received(1)
                    .DeleteSubscription("smoketest", uniqueId);
            }
            else
            {
                ((IQueueService)messengerService)
                    .Received(1)
                    .DeleteQueue(uniqueId);
            }

            messengerService
                .Received(1)
                .ReceiveMessages<SmokeResponse>(entityPathBase,
                    _timeout);
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
