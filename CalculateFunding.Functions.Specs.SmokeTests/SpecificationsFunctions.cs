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
        private static ISpecificationIndexingService _specificationIndexerService;
        private static ILogger _logger;
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("specs");

            _logger = CreateLogger();

            _specificationsService = CreateSpecificationService();
            _userProfileProvider = CreateUserProfileProvider();
            _specificationIndexerService = CreateSpecificationIndexerService();
        }

        [TestMethod]
        public async Task OnAddRelationshipEvent_SmokeTestSucceeds()
        {
            OnAddRelationshipEvent onAddRelationshipEvent = new OnAddRelationshipEvent(_logger,
                _specificationsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification,
                async(Message smokeResponse) => await onAddRelationshipEvent.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnDeleteSpecifications_SmokeTestSucceeds()
        {
            OnDeleteSpecifications onDeleteSpecifications = new OnDeleteSpecifications(_logger,
                _specificationsService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                 _userProfileProvider,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.DeleteSpecifications,
                async(Message smokeResponse) => await onDeleteSpecifications.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }
        
        [TestMethod]
        public async Task OnReIndexSpecification_SmokeTestSucceeds()
        {
            OnReIndexSpecification onReIndexSpecifications = new OnReIndexSpecification(_logger,
                _specificationIndexerService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.ReIndexSingleSpecification,
                async(Message smokeResponse) => await onReIndexSpecifications.Run(smokeResponse), useSession: true);

            response
                .Should()
                .NotBeNull();
        }

        private static ILogger CreateLogger() => Substitute.For<ILogger>();

        private static ISpecificationsService CreateSpecificationService() => Substitute.For<ISpecificationsService>();

        private static IUserProfileProvider CreateUserProfileProvider() => Substitute.For<IUserProfileProvider>();

        private static ISpecificationIndexingService CreateSpecificationIndexerService() => Substitute.For<ISpecificationIndexingService>();
    }
}
