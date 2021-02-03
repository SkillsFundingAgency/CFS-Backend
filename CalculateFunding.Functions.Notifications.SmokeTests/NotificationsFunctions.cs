using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Notifications.Interfaces;
using CalculateFunding.Tests.Common;
using CalculateFunding.Tests.Common.Helpers;
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
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("notifications");

            _logger = CreateLogger();
            _notificationService = CreateNotificationService();
            _userProfileProvider = CreateUserProfileProvider();
        }

        [TestMethod]
        public async Task OnNotoficationEventTriggered_SmokeTestSucceeds()
        {
            OnNotificationEventTrigger onNotificationEventTrigger = new OnNotificationEventTrigger(_logger,
                _notificationService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                AppConfigurationHelper.CreateConfigurationRefresherProvider(),
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.JobNotificationsToSignalR,
                async(Message smokeResponse) => await onNotificationEventTrigger.Run(smokeResponse, null),
                ServiceBusConstants.TopicNames.JobNotifications);

            response
                .Should()
                .NotBeNull();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
        
        private static INotificationService CreateNotificationService()
        {
            return Substitute.For<INotificationService>();
        }

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }
    }
}
