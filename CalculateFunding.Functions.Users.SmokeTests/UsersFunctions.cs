using CalculateFunding.Common.Models;
using CalculateFunding.Functions.Users.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Users.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Users.SmokeTests
{
    [TestClass]
    public class TestEngineFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static IFundingStreamPermissionService _fundingStreamPermissionService;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("users");

            _logger = CreateLogger();
            _fundingStreamPermissionService = CreateFundingStreamPermissionService();
        }

        [TestMethod]
        public async Task OnEditSpecificationEvent_SmokeTestSucceeds()
        {
            OnEditSpecificationEvent onEditSpecificationEvent = new OnEditSpecificationEvent(_logger,
                _fundingStreamPermissionService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(OnEditSpecificationEvent.FunctionName,
                ServiceBusConstants.TopicSubscribers.UpdateUsersForEditSpecification,
                (Message smokeResponse) => onEditSpecificationEvent.Run(smokeResponse),
                ServiceBusConstants.TopicNames.EditSpecification);

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IFundingStreamPermissionService CreateFundingStreamPermissionService()
        {
            return Substitute.For<IFundingStreamPermissionService>();
        }
    }
}
