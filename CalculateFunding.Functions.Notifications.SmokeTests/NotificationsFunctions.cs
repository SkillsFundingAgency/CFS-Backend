using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Notifications.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Notifications.SmokeTests
{
    [TestClass]
    public class NotificationsFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static INotificationService _notificationService;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("notifications");

            _logger = CreateLogger();
            _notificationService = CreateNotificationService();
        }

        [TestMethod]
        public async Task OnNotoficationEventTriggered_SmokeTestSucceeds()
        {
            OnNotificationEventTrigger onNotificationEventTrigger = new OnNotificationEventTrigger(_logger,
                _notificationService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(OnNotificationEventTrigger.FunctionName,
                ServiceBusConstants.TopicSubscribers.JobNotificationsToSignalR,
                (Message smokeResponse) => onNotificationEventTrigger.Run(smokeResponse, null),
                ServiceBusConstants.TopicNames.JobNotifications);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
        
        private static INotificationService CreateNotificationService()
        {
            return Substitute.For<INotificationService>();
        }
    }
}
