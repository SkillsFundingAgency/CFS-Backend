using CalculateFunding.Common.Models;
using CalculateFunding.Functions.Jobs.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
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

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("jobs");

            _logger = CreateLogger();
            _jobManagementService = CreateJobManagementService();
        }

        [TestMethod]
        public async Task OnDeleteJobs_SmokeTestSucceeds()
        {
            OnDeleteJobs onDeleteJobs = new OnDeleteJobs(_logger,
                _jobManagementService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.DeleteJobs,
                (Message smokeResponse) => onDeleteJobs.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnJobNotification_SmokeTestSucceeds()
        {
            OnJobNotification onJobNotification = new OnJobNotification(_logger,
                _jobManagementService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.UpdateJobsOnCompletion,
                (Message smokeResponse) => onJobNotification.Run(smokeResponse),
                ServiceBusConstants.TopicNames.JobNotifications);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
        
        private static IJobManagementService CreateJobManagementService()
        {
            return Substitute.For<IJobManagementService>();
        }
    }
}
