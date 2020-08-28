using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.Jobs.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Jobs.SmokeTests
{
    [TestClass]
    public class JobsFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static IJobManagementService _jobManagementService;
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("jobs");

            _logger = CreateLogger();
            _jobManagementService = CreateJobManagementService();
            _userProfileProvider = CreateUserProfileProvider();
        }

        [TestMethod]
        public async Task OnJobNotification_SmokeTestSucceeds()
        {
            OnJobNotification onJobNotification = new OnJobNotification(_logger,
                _jobManagementService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateJobsOnCompletion,
                (Message smokeResponse) => onJobNotification.Run(smokeResponse),
                ServiceBusConstants.TopicNames.JobNotifications);

            response
                .Should()
                .NotBeNull();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
        
        private static IJobManagementService CreateJobManagementService()
        {
            return Substitute.For<IJobManagementService>();
        }

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }
    }
}
