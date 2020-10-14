using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.Providers.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Providers.SmokeTests
{
    [TestClass]
    public class ProvidersFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static IScopedProvidersService _scopedProvidersService;
        private static IProviderSnapshotDataLoadService _providerSnapshotDataLoadService;
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("datasets");

            _logger = CreateLogger();

            _scopedProvidersService = CreateScopedProvidersService();
            _providerSnapshotDataLoadService = CreateProviderSnapshotDataLoadService();
            _userProfileProvider = CreateUserProfileProvider();
        }

        [TestMethod]
        public async Task OnPopulateScopedProvidersEvent_SmokeTestSucceeds()
        {
            OnPopulateScopedProvidersEventTrigger onPopulateScopedProvidersEventTrigger = new OnPopulateScopedProvidersEventTrigger(_logger,
                _scopedProvidersService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.PopulateScopedProviders,
                (Message smokeResponse) => onPopulateScopedProvidersEventTrigger.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task OnProviderSnapshotDataLoadEvent_SmokeTestSucceeds()
        {
            OnProviderSnapshotDataLoadEventTrigger onProviderSnapshotDataLoadEventTrigger = new OnProviderSnapshotDataLoadEventTrigger(_logger,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                _providerSnapshotDataLoadService,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.ProviderSnapshotDataLoad,
                (Message smokeResponse) => onProviderSnapshotDataLoadEventTrigger.Run(smokeResponse), useSession: true);

            response
                .Should()
                .NotBeNull();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IScopedProvidersService CreateScopedProvidersService()
        {
            return Substitute.For<IScopedProvidersService>();
        }

        private static IProviderSnapshotDataLoadService CreateProviderSnapshotDataLoadService()
        {
            return Substitute.For<IProviderSnapshotDataLoadService>();
        }

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }
    }
}
