﻿using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Options;
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
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.ServiceBus.Options;
using CalculateFunding.Common.ServiceBus;

namespace CalculateFunding.Tests.Common
{
    public class SmokeTestBase
    {
        protected static bool IsDevelopment;
        private static bool _useMocking;
        
        private static TimeSpan _timeout;
        
        protected static IServiceCollection Services;

        protected static void SetupTests(string serviceName)
        {
            Guard.IsNullOrWhiteSpace(serviceName, nameof(serviceName));

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("local.settings.json")
                .Build();

            Services = new ServiceCollection();

            Services.AddSingleton(configuration);

            IsDevelopment = configuration["ASPNETCORE_ENVIRONMENT"] == "Development";

            _useMocking = configuration["USE_MOCKING"] == "true";

            Services.AddSingleton((ctx) =>
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

            _timeout = TimeSpan.FromSeconds(IsDevelopment ? 10 : 60);
        }

        public async Task<SmokeResponse> RunSmokeTest(string queueName,
            Func<Message, Task> action,
            string topicName = null,
            bool useSession = false)
        {
            ServiceProvider serviceProvider = Services.BuildServiceProvider();

            if (IsDevelopment && !_useMocking)
            {
                using (AzureStorageEmulatorAutomation azureStorageEmulatorAutomation = new AzureStorageEmulatorAutomation())
                {
                    await azureStorageEmulatorAutomation.Init();
                    await azureStorageEmulatorAutomation.Start();

                    return await RunSmokeTest(serviceProvider.GetRequiredService<IMessengerService>(),
                    queueName,
                    action,
                    topicName,
                    useSession);
                }
            }
            else
            {
                return await RunSmokeTest(serviceProvider.GetRequiredService<IMessengerService>(),
                    queueName,
                    action,
                    topicName,
                    useSession);
            }
        }

        private async Task<SmokeResponse> RunSmokeTest(IMessengerService messengerService,
            string queueName,
            Func<Message, Task> action,
            string topicName,
            bool useSession)
        {
            Guard.IsNullOrWhiteSpace(queueName, nameof(queueName));
            Guard.ArgumentNotNull(action, nameof(action));

            string uniqueId = Guid.NewGuid().ToString();

            IDictionary<string, string> properties = new Dictionary<string, string> { { "smoketest", uniqueId } };

            string entityPathBase = !IsDevelopment ? $"{ServiceBusConstants.TopicNames.SmokeTest}/Subscriptions/{uniqueId}" : uniqueId;

            if (_useMocking)
            {
                MockReceiveMessages(messengerService, uniqueId, entityPathBase, queueName);
            }

            try
            {
                if(!IsDevelopment)
                {
                    await ((IServiceBusService)messengerService).CreateSubscription("smoketest", uniqueId, new TimeSpan(1,0,0,0));
                }

                if (!IsDevelopment && topicName != null)
                {
                    if (useSession)
                    {
                        await messengerService.SendToTopic(topicName,
                            uniqueId,
                            properties,
                            sessionId: uniqueId);
                    }
                    else
                    {
                        await messengerService.SendToTopic(topicName,
                            uniqueId,
                            properties);
                    }
                }
                else
                {
                    if (useSession)
                    {
                        await messengerService.SendToQueue(queueName,
                        uniqueId,
                        properties,
                        sessionId: uniqueId);
                    }
                    else
                    {
                        await messengerService.SendToQueue(queueName,
                        uniqueId,
                        properties);
                    }
                }

                if (IsDevelopment)
                {
                    IEnumerable<string> smokeResponsesFromFunction = await messengerService.ReceiveMessages<string>(queueName,
                        _timeout);

                    Message message = new Message();
                    message.UserProperties.Add("smoketest", smokeResponsesFromFunction?.FirstOrDefault(_ => _ == uniqueId));

                    action = _useMocking ? async(msg) =>
                    {
                        await Task.FromResult(msg.UserProperties["smoketest"].Equals(uniqueId));
                    }
                    : action;

                    await action(message);
                }

                return await messengerService.ReceiveMessage<SmokeResponse>(entityPathBase,_ => _.InvocationId == uniqueId,
                    _timeout);
            }
            finally
            {
                if (!IsDevelopment)
                {
                    await ((IServiceBusService)messengerService).DeleteSubscription("smoketest", uniqueId);
                }
                else
                {
                    await ((IQueueService)messengerService).DeleteQueue(uniqueId);
                }

                if (_useMocking)
                {
                    CheckServiceBusCalls(messengerService, uniqueId, queueName, topicName, entityPathBase, useSession);
                }
            }
        }

        private static void MockReceiveMessages(IMessengerService messengerService, string uniqueId, string entityPathBase, string queueName)
        {
            messengerService.ReceiveMessages<string>(queueName,
                        _timeout)
                .Returns(new string[] { uniqueId });

            messengerService.ReceiveMessage<SmokeResponse>(entityPathBase,
                    Arg.Any<Predicate<SmokeResponse>>(),
                    _timeout)
                .Returns(new SmokeResponse { InvocationId = uniqueId });
        }

        private static void CheckServiceBusCalls(IMessengerService messengerService, string uniqueId, string queueName, string topicName, string entityPathBase, bool useSession)
        {
            if (!IsDevelopment && topicName != null)
            {
                if (useSession)
                {
                    messengerService
                    .Received(1)
                    .SendToTopic(topicName,
                    uniqueId,
                    Arg.Any<Dictionary<string, string>>(),
                    sessionId: uniqueId);
                }
                else
                {
                    messengerService
                    .Received(1)
                    .SendToTopic(topicName,
                    uniqueId,
                    Arg.Any<Dictionary<string, string>>());
                }
            }
            else
            {
                if (useSession)
                {
                    messengerService
                        .Received(1)
                        .SendToQueue(queueName,
                        uniqueId,
                        Arg.Any<Dictionary<string, string>>(),
                        sessionId:uniqueId);
                }
                else
                {
                    messengerService
                        .Received(1)
                        .SendToQueue(queueName,
                        uniqueId,
                        Arg.Any<Dictionary<string, string>>());
                }
            }

            if (!IsDevelopment)
            {
                ((IServiceBusService)messengerService)
                .Received(1)
                .CreateSubscription("smoketest", uniqueId, Arg.Is<TimeSpan>(_ => _.Days == 1));

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
                .ReceiveMessage<SmokeResponse>(entityPathBase,
                    Arg.Any<Predicate<SmokeResponse>>(),
                    _timeout);
        }

        private static IMessengerService AddServiceBus(IConfiguration configuration, string serviceName)
        {
            if (IsDevelopment)
            {
                CalculateFunding.Common.ServiceBus.QueueClient queueClient = new CalculateFunding.Common.ServiceBus.QueueClient("UseDevelopmentStorage=true");
                return new QueueMessengerService(queueClient, serviceName);
            }
            else
            {
                ServiceBusSettings serviceBusSettings = new ServiceBusSettings();

                configuration.Bind("ServiceBusSettings", serviceBusSettings);

                MessageReceiverFactory messageReceiverFactory = new MessageReceiverFactory(serviceBusSettings.ConnectionString);
                ManagementClient managementClient = new ManagementClient(serviceBusSettings.ConnectionString);

                return new MessengerService(serviceBusSettings, managementClient, messageReceiverFactory, serviceName);
            }
        }
    }
}
