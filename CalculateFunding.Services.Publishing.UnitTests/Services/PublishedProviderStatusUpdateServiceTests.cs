using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class PublishedProviderStatusUpdateServiceTests
    {
        private Reference author = new Reference("authorid", "authorname");

        [TestMethod]
        public void UpdatePublishedProviderStatus_GivenNoPublishedProviderCreateVersionRequestsAssembled_ThrowsNonRetriableException()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = Enumerable.Empty<PublishedProvider>();

            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests = 
                Enumerable.Empty<PublishedProviderCreateVersionRequest>();

            ILogger logger = CreateLogger();

            IPublishedProviderVersioningService providerVersioningService = CreateVersioningService();
            providerVersioningService
                .AssemblePublishedProviderCreateVersionRequests(Arg.Is(publishedProviders), Arg.Is(author), Arg.Is(PublishedProviderStatus.Approved))
                .Returns(publishedProviderCreateVersionRequests);

            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService, logger);

            string errorMessage = "No published providers were assemebled for updating.";

            //Assert
            Func<Task> test = async () => await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved);

            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(errorMessage));
        }

        [TestMethod]
        public void UpdatePublishedProviderStatus_GivenAssembledPublishedProviderCreateVersionRequestButCreateVersionCausesException_ThrowsRetriableException()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = Enumerable.Empty<PublishedProvider>();

            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests = new[]
            {
                new PublishedProviderCreateVersionRequest()
            };

            ILogger logger = CreateLogger();

            IPublishedProviderVersioningService providerVersioningService = CreateVersioningService();

            providerVersioningService
                .AssemblePublishedProviderCreateVersionRequests(Arg.Is(publishedProviders), Arg.Is(author), Arg.Is(PublishedProviderStatus.Approved))
                .Returns(publishedProviderCreateVersionRequests);

            providerVersioningService
                .When(x => x.CreateVersions(Arg.Is(publishedProviderCreateVersionRequests)))
                .Do(x => { throw new Exception(); });

            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService, logger);

            string errorMessage = $"Failed to create versions when updating status:' {PublishedProviderStatus.Approved}' on published providers.";

            //Assert
            Func<Task> test = async () => await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved);

            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is(errorMessage));
        }

        [TestMethod]
        public void UpdatePublishedProviderStatus_GivenVersionsCreatedButSavingCausesException_ThrowsRetriableException()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider()
            };

            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests = new[]
            {
                new PublishedProviderCreateVersionRequest()
            };

            ILogger logger = CreateLogger();

            IPublishedProviderVersioningService providerVersioningService = CreateVersioningService();

            providerVersioningService
                .AssemblePublishedProviderCreateVersionRequests(Arg.Is(publishedProviders), Arg.Is(author), Arg.Is(PublishedProviderStatus.Approved))
                .Returns(publishedProviderCreateVersionRequests);

            providerVersioningService
                .CreateVersions(Arg.Is(publishedProviderCreateVersionRequests))
                .Returns(publishedProviders);

            providerVersioningService
                .When(x => x.SaveVersions(Arg.Is(publishedProviders)))
                .Do(x => { throw new Exception(); });

            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService, logger);

            string errorMessage = $"Failed to save versions when updating status:' {PublishedProviderStatus.Approved}' on published providers.";

            //Assert
            Func<Task> test = async () => await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved);

            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is(errorMessage));
        }

        [TestMethod]
        public async Task UpdatePublishedProviderStatus_GivenNoVersionsCreated_DoesNotSave()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = Enumerable.Empty<PublishedProvider>();

            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests = new[]
            {
                new PublishedProviderCreateVersionRequest()
            };

            ILogger logger = CreateLogger();

            IPublishedProviderVersioningService providerVersioningService = CreateVersioningService();

            providerVersioningService
                .AssemblePublishedProviderCreateVersionRequests(Arg.Is(publishedProviders), Arg.Is(author), Arg.Is(PublishedProviderStatus.Approved))
                .Returns(publishedProviderCreateVersionRequests);

            providerVersioningService
                .CreateVersions(Arg.Is(publishedProviderCreateVersionRequests))
                .Returns(publishedProviders);

            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService, logger);

            string errorMessage = $"Failed to save versions when updating status:' {PublishedProviderStatus.Approved}' on published providers.";

            //Assert
            await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved);

            await
                providerVersioningService
                    .DidNotReceive()
                    .SaveVersions(Arg.Any<IEnumerable<PublishedProvider>>());
        }

        private static PublishedProviderStatusUpdateService CreatePublishedProviderStatusUpdateService(
            IPublishedProviderVersioningService publishedProviderVersioningService = null, ILogger logger = null)
        {
            return new PublishedProviderStatusUpdateService(
                    publishedProviderVersioningService ?? CreateVersioningService(),
                    logger ?? CreateLogger()
                );
        }

        private static IPublishedProviderVersioningService CreateVersioningService()
        {
            return Substitute.For<IPublishedProviderVersioningService>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
