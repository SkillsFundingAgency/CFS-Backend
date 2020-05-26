using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.CosmosDbScaling.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.CosmosDbScaling.SmokeTests
{
    [TestClass]
    public class CosmosDbScalingFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static ICosmosDbScalingService _cosmosDbScalingService;
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("cosmosdbscaling");

            _logger = CreateLogger();

            _cosmosDbScalingService = CreateCosmosDbScalingService();

            _userProfileProvider = CreateUserProfileProvider();
        }

        [TestMethod]
        public async Task OnScaleUpCosmosDbCollection_SmokeTestSucceeds()
        {
            OnScaleUpCosmosDbCollection onScaleUpCosmosDbCollection = new OnScaleUpCosmosDbCollection(_logger,
                _cosmosDbScalingService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.TopicSubscribers.ScaleUpCosmosdbCollection,
                (Message smokeResponse) => onScaleUpCosmosDbCollection.Run(smokeResponse),
                ServiceBusConstants.TopicNames.JobNotifications);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static ICosmosDbScalingService CreateCosmosDbScalingService()
        {
            return Substitute.For<ICosmosDbScalingService>();
        }

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }
    }
}
