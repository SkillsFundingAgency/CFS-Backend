using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.Specs.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Specs.SmokeTests
{
    [TestClass]
    public class SpecificationsFunctions : SmokeTestBase
    {
        private static ISpecificationsService _specificationsService;
        private static ILogger _logger;
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("specs");

            _logger = CreateLogger();

            _specificationsService = CreateSpecificationService();
            _userProfileProvider = CreateUserProfileProvider();
        }

        [TestMethod]
        public async Task OnAddRelationshipEvent_SmokeTestSucceeds()
        {
            OnAddRelationshipEvent onAddRelationshipEvent = new OnAddRelationshipEvent(_logger,
                _specificationsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification,
                (Message smokeResponse) => onAddRelationshipEvent.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnDeleteSpecifications_SmokeTestSucceeds()
        {
            OnDeleteSpecifications onDeleteSpecifications = new OnDeleteSpecifications(_logger,
                _specificationsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                IsDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.DeleteSpecifications,
                (Message smokeResponse) => onDeleteSpecifications.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static ISpecificationsService CreateSpecificationService()
        {
            return Substitute.For<ISpecificationsService>();
        }

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }
    }
}
