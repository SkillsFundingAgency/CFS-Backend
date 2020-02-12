using CalculateFunding.Common.Models;
using CalculateFunding.Functions.Publishing.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Publishing.SmokeTests
{
    [TestClass]
    public class PublishingFunctions : SmokeTestBase
    {
        private static ILogger _logger;
        private static IApproveService _approveService;
        private static IRefreshService _refreshService;
        private static IPublishService _publishService;
        private static IPublishedProviderReIndexerService _publishedProviderReIndexerService;
        private static IDeletePublishedProvidersService _deletePublishedProvidersService;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("calcengine");

            _logger = CreateLogger();
            _approveService = CreateApproveService();
            _publishService = CreatePublishService();
            _refreshService = CreateRefreshService();
            _publishedProviderReIndexerService = CreatePublishedProviderReIndexerService();
            _deletePublishedProvidersService = CreateDeletePublishedProvidersService();
        }

        [TestMethod]
        public async Task OnApproveFunding_SmokeTestSucceeds()
        {
            OnApproveFunding onApproveFunding = new OnApproveFunding(_logger,
                _approveService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.PublishingApproveFunding,
                (Message smokeResponse) => onApproveFunding.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnPublishFunding_SmokeTestSucceeds()
        {
            OnPublishFunding onPublishFunding = new OnPublishFunding(_logger,
                _publishService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.PublishingPublishFunding,
                (Message smokeResponse) => onPublishFunding.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnRefreshFunding_SmokeTestSucceeds()
        {
            OnRefreshFunding onRefreshFunding = new OnRefreshFunding(_logger,
                _refreshService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.PublishingRefreshFunding,
                (Message smokeResponse) => onRefreshFunding.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnReIndexPublishedProvider_SmokeTestSucceeds()
        {
            OnReIndexPublishedProviders onReIndexPublishedProviders = new OnReIndexPublishedProviders(_logger,
                _publishedProviderReIndexerService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.PublishingReIndexPublishedProviders,
                (Message smokeResponse) => onReIndexPublishedProviders.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        [TestMethod]
        public async Task OnDeletePublishedProviders_SmokeTestSucceeds()
        {
            OnDeletePublishedProviders onDeletePublishedProvider = new OnDeletePublishedProviders(_logger,
                _deletePublishedProvidersService,
                _services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _isDevelopment);

            (IEnumerable<SmokeResponse> responses, string uniqueId) = await RunSmokeTest(ServiceBusConstants.QueueNames.DeletePublishedProviders,
                (Message smokeResponse) => onDeletePublishedProvider.Run(smokeResponse));

            responses
                .Should()
                .Contain(_ => _.InvocationId == uniqueId);
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
        
        private static IApproveService CreateApproveService()
        {
            return Substitute.For<IApproveService>();
        }

        private static IRefreshService CreateRefreshService()
        {
            return Substitute.For<IRefreshService>();
        }

        private static IPublishService CreatePublishService()
        {
            return Substitute.For<IPublishService>();
        }
        
        private static IPublishedProviderReIndexerService CreatePublishedProviderReIndexerService()
        {
            return Substitute.For<IPublishedProviderReIndexerService>();
        }

        private static IDeletePublishedProvidersService CreateDeletePublishedProvidersService()
        {
            return Substitute.For<IDeletePublishedProvidersService>();
        }
    }
}
