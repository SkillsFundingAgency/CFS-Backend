using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.Policy.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Functions.Policy.SmokeTests
{
    [TestClass]
    public class PolicyFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static ITemplatesReIndexerService _templatesReIndexerService;
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("datasets");

            _logger = CreateLogger();

            _templatesReIndexerService = CreateTemplatesReIndexerService();
            _userProfileProvider = CreateUserProfileProvider();
        }

        [TestMethod]
        public async Task OnReIndexTemplates_SmokeTestSucceeds()
        {
            OnReIndexTemplates onReIndexTemplates = new OnReIndexTemplates(_logger,
                _templatesReIndexerService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.PolicyReIndexTemplates,
                async (Message smokeResponse) => await onReIndexTemplates.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static ITemplatesReIndexerService CreateTemplatesReIndexerService()
        {
            return Substitute.For<ITemplatesReIndexerService>();
        }

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }
    }
}
