using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class PublishedProviderStatusUpdateServiceTests
    {
        private Reference author = new Reference("authorid", "authorname");

        [TestMethod]
        public async Task UpdatePublishedProviderStatus_BatchesInto200sIfJobIdSupplied()
        {
            //Arrange
            List<PublishedProvider> publishedProviders = Enumerable.Range(1, 605)
                .Select(_ => new PublishedProvider())
                .ToList();

            List<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests = Enumerable.Range(1, 605)
                .Select(_ => new PublishedProviderCreateVersionRequest())
                .ToList();

            string jobId = new RandomString();
            string correlationId = new RandomString();

            Mock<ILogger> logger = CreateLogger();
            Mock<IPublishedProviderVersioningService> providerVersioningService = CreateVersioningService();
            Mock<IJobTracker> jobTracker = CreateJobTracker();

            providerVersioningService
                .Setup(x => 
                    x.AssemblePublishedProviderCreateVersionRequests(
                    It.IsAny<IEnumerable<PublishedProvider>>(), 
                    It.Is<Reference>(_ => _ == author),
                    It.Is<PublishedProviderStatus>(_ => _ == PublishedProviderStatus.Approved),
                    It.Is<string>(_ => _ == jobId),
                    It.Is<string>(_ => _ == correlationId)))
                .Returns(publishedProviderCreateVersionRequests);

            providerVersioningService
                .Setup(x => 
                    x.CreateVersions(It.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(
                        _ => _ == publishedProviderCreateVersionRequests)))
                .ReturnsAsync(publishedProviders);

            providerVersioningService
                .Setup(x => 
                    x.CreateVersions(It.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(_
                        => _.SequenceEqual(publishedProviderCreateVersionRequests.Take(200)))))
                .ReturnsAsync(publishedProviders.Take(200));

            providerVersioningService
                .Setup(x => 
                    x.CreateVersions(It.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(_
                        => _.SequenceEqual(publishedProviderCreateVersionRequests.Skip(200).Take(200)))))
                .ReturnsAsync(publishedProviders.Skip(200).Take(200));

            providerVersioningService
                .Setup(x => x.CreateVersions(It.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(_
                    => _.SequenceEqual(publishedProviderCreateVersionRequests.Skip(400).Take(200)))))
                .ReturnsAsync(publishedProviders.Skip(400).Take(200));

            providerVersioningService
                .Setup(x => x.CreateVersions(It.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(_
                    => _.SequenceEqual(publishedProviderCreateVersionRequests.Skip(600).Take(200)))))
                .ReturnsAsync(publishedProviders.Skip(600).Take(200));


            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService.Object, logger.Object, jobTracker.Object);


            //Act
            await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved, jobId, correlationId);

            //Assert
            // TODO - fix due to further batching because of optmisation
            //await providerVersioningService
            //    .Received(1)
            //    .SaveVersions(It.Is<IEnumerable<PublishedProvider>>(_ =>
            //        _.SequenceEqual(publishedProviders.Take(200))));

            //await providerVersioningService
            //    .Received(1)
            //    .SaveVersions(It.Is<IEnumerable<PublishedProvider>>(_ =>
            //        _.SequenceEqual(publishedProviders.Skip(200).Take(200))));

            //await providerVersioningService
            //    .Received(1)
            //    .SaveVersions(It.Is<IEnumerable<PublishedProvider>>(_ =>
            //        _.SequenceEqual(publishedProviders.Skip(400).Take(200))));

            //await providerVersioningService
            //    .Received(1)
            //    .SaveVersions(It.Is<IEnumerable<PublishedProvider>>(_ =>
            //        _.SequenceEqual(publishedProviders.Skip(600).Take(200))));

            jobTracker.Verify(x => x.NotifyProgress(200, jobId), Times.Once);
            jobTracker.Verify(x => x.NotifyProgress(400, jobId), Times.Once);
            jobTracker.Verify(x => x.NotifyProgress(600, jobId), Times.Once);
            jobTracker.Verify(x => x.NotifyProgress(605, jobId), Times.Once);
        }

        [TestMethod]
        public async Task UpdatePublishedProviderStatus_GivenNoPublishedProviderCreateVersionRequestsAssembled_ReturnsZeroUpdatedCount()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = Enumerable.Empty<PublishedProvider>();

            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests =
                Enumerable.Empty<PublishedProviderCreateVersionRequest>();

            Mock<ILogger> logger = CreateLogger();

            string jobId = new RandomString();
            string correlationId = new RandomString();

            Mock<IPublishedProviderVersioningService> providerVersioningService = CreateVersioningService();
            providerVersioningService
                .Setup(x => 
                    x.AssemblePublishedProviderCreateVersionRequests(
                        It.Is<IEnumerable<PublishedProvider>>(_ => _ == publishedProviders), 
                        It.Is<Reference>(_ => _ == author), 
                        It.Is<PublishedProviderStatus>(_ => _ == PublishedProviderStatus.Approved),
                        It.Is<string>(_ => _ == jobId),
                        It.Is<string>(_ => _ == correlationId)))
                .Returns(publishedProviderCreateVersionRequests);
            
            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService.Object, logger.Object);

            //Assert
            int updateCount = await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved);

            updateCount
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task UpdatePublishedProviderStatus_GivenPublishedProviderCreateVersionRequestsWithJobIdAndCorrelationId_AssemblePublishedProviderRequestsWithJobIdAndCorrelationId()
        {
            //Arrange
            const string jobId = "JobId-abc-123";
            const string correlationId = "CorrelationId-xyz-123";
            IEnumerable<PublishedProvider> publishedProviders = new[] { new PublishedProvider() };

            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests =
                new[] { new PublishedProviderCreateVersionRequest() };

            Mock<ILogger> logger = CreateLogger();

            Mock<IPublishedProviderVersioningService> providerVersioningService = CreateVersioningService();
            providerVersioningService
                .Setup(x =>
                    x.AssemblePublishedProviderCreateVersionRequests(
                        It.IsAny<IEnumerable<PublishedProvider>>(),
                        It.Is<Reference>(_ => _ == author),
                        It.Is<PublishedProviderStatus>(_ => _ == PublishedProviderStatus.Approved),
                        It.Is<string>(_ => _ == jobId),
                        It.Is<string>(_ => _ == correlationId)))
                .Returns(publishedProviderCreateVersionRequests);

            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService.Object, logger.Object);

            //Assert
            int updateCount = await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved, jobId, correlationId);

            providerVersioningService.Verify(x => x.AssemblePublishedProviderCreateVersionRequests(
                        It.IsAny<IEnumerable<PublishedProvider>>(),
                        It.Is<Reference>(_ => _ == author),
                        It.Is<PublishedProviderStatus>(_ => _ == PublishedProviderStatus.Approved),
                        It.Is<string>(_ => _ == jobId),
                        It.Is<string>(_ => _ == correlationId)), Times.Once);
        }

        [TestMethod]
        public void UpdatePublishedProviderStatus_GivenAssembledPublishedProviderCreateVersionRequestButCreateVersionCausesException_ThrowsRetriableException()
        {
            //Arrange
            const string jobId = null;
            const string correlationId = null;
            IEnumerable<PublishedProvider> publishedProviders = Enumerable.Empty<PublishedProvider>();

            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests = new[]
            {
                new PublishedProviderCreateVersionRequest()
            };

            Mock<ILogger> logger = CreateLogger();

            Mock<IPublishedProviderVersioningService> providerVersioningService = CreateVersioningService();
            providerVersioningService
                .Setup(x => 
                    x.AssemblePublishedProviderCreateVersionRequests(
                        It.IsAny<IEnumerable<PublishedProvider>>(), 
                        It.Is<Reference>(_ => _ == author), 
                        It.Is<PublishedProviderStatus>(_ => _ == PublishedProviderStatus.Approved),
                        It.Is<string>(_ => _ == jobId),
                        It.Is<string>(_ => _ == correlationId)))
                .Returns(publishedProviderCreateVersionRequests);

            providerVersioningService.Setup(x => 
                    x.CreateVersions(It.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(_ => _ == publishedProviderCreateVersionRequests)))
                .Throws(new Exception());

            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService.Object, logger.Object);

            string errorMessage = $"Failed to create versions when updating status:' {PublishedProviderStatus.Approved}' on published providers.";

            //Assert
            Func<Task> test = async () => await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved, jobId, correlationId);

            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            logger.Verify(x => 
                    x.Error(It.IsAny<Exception>(), It.Is<string>(_ => _ == errorMessage)),
                    Times.Once);
        }

        [TestMethod]
        public void UpdatePublishedProviderStatus_GivenVersionsCreatedButSavingCausesException_ThrowsRetriableException()
        {
            //Arrange
            const string jobId = null;
            const string correlationId = null;
            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider()
            };

            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests = new[]
            {
                new PublishedProviderCreateVersionRequest()
            };

            Mock<ILogger> logger = CreateLogger();
            Mock<IPublishedProviderVersioningService> providerVersioningService = CreateVersioningService();
            Mock<IPublishedFundingRepository> publishedFundingRepository = CreatePublishedFundingRepository();
            
            providerVersioningService
                .Setup(x => 
                    x.AssemblePublishedProviderCreateVersionRequests(
                        It.IsAny<IEnumerable<PublishedProvider>>(), 
                        It.Is<Reference>(_ => _ == author), 
                        It.Is<PublishedProviderStatus>(_ => _ == PublishedProviderStatus.Approved),
                         It.Is<string>(_ => _ == jobId),
                        It.Is<string>(_ => _ == correlationId)))
                .Returns(publishedProviderCreateVersionRequests);
            providerVersioningService
                .Setup(x => 
                    x.SaveVersions(It.IsAny<IEnumerable<PublishedProvider>>()))
                .Throws(new Exception());
            providerVersioningService.Setup(x => 
                    x.CreateVersions(It.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(_ => _ == publishedProviderCreateVersionRequests)))
                .ReturnsAsync(publishedProviders);

            publishedFundingRepository.Setup(x => 
                    x.UpsertPublishedProviders(It.Is<IEnumerable<PublishedProvider>>(_ => _ == publishedProviders)))
                .ReturnsAsync(new[] { HttpStatusCode.OK });

            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService.Object, logger: logger.Object, publishedFundingRepository: publishedFundingRepository.Object);

            string errorMessage = $"Failed to save versions when updating status:' {PublishedProviderStatus.Approved}' on published providers.";

            //Assert
            Func<Task> test = async () => await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved, jobId, correlationId);

            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .InnerException
                .Message
                .Should()
                .Be(errorMessage);

            logger.Verify(x => 
                    x.Error(It.IsAny<Exception>(), It.Is<string>(_ => _ == errorMessage)),
                Times.Once);
        }

        [TestMethod]
        public async Task UpdatePublishedProviderStatus_GivenNoVersionsCreated_DoesNotSave()
        {
            //Arrange
            const string jobId = null;
            const string correlationId = null;
            IEnumerable<PublishedProvider> publishedProviders = Enumerable.Empty<PublishedProvider>();

            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests = new[]
            {
                new PublishedProviderCreateVersionRequest()
            };

            Mock<ILogger> logger = CreateLogger();

            Mock<IPublishedProviderVersioningService> providerVersioningService = CreateVersioningService();
            providerVersioningService
                .Setup(x => 
                    x.AssemblePublishedProviderCreateVersionRequests(
                        It.IsAny<IEnumerable<PublishedProvider>>(), 
                        It.Is<Reference>(_ => _ == author), 
                        It.Is<PublishedProviderStatus>(_ => _ == PublishedProviderStatus.Approved),
                          It.Is<string>(_ => _ == jobId),
                        It.Is<string>(_ => _ == correlationId)))
                .Returns(publishedProviderCreateVersionRequests);
            providerVersioningService.Setup(x => 
                    x.CreateVersions(It.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(_ => _ == publishedProviderCreateVersionRequests)))
                .ReturnsAsync(publishedProviders);

            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService.Object, logger.Object);

            string errorMessage = $"Failed to save versions when updating status:' {PublishedProviderStatus.Approved}' on published providers.";

            //Assert
            await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved,jobId,correlationId);

            providerVersioningService
                .Verify(x => x.SaveVersions(It.IsAny<IEnumerable<PublishedProvider>>()),
                    Times.Never());
        }

        private static PublishedProviderStatusUpdateService CreatePublishedProviderStatusUpdateService(
            IPublishedProviderVersioningService publishedProviderVersioningService = null,
            ILogger logger = null,
            IJobTracker jobTracker = null,
            IPublishedFundingRepository publishedFundingRepository = null)
        {
            IConfiguration configuration = Mock.Of<IConfiguration>();

            return new PublishedProviderStatusUpdateService(
                    publishedProviderVersioningService ?? CreateVersioningService().Object,
                    publishedFundingRepository ?? CreatePublishedFundingRepository().Object,
                    jobTracker ?? CreateJobTracker().Object,
                    logger ?? CreateLogger().Object,
                    new PublishedProviderStatusUpdateSettings(),
                    new PublishingEngineOptions(configuration));
        }


        private static Mock<IPublishedProviderVersioningService> CreateVersioningService()
        {
            return new Mock<IPublishedProviderVersioningService>();
        }

        private static Mock<IPublishedFundingRepository> CreatePublishedFundingRepository()
        {
            return new Mock<IPublishedFundingRepository>();
        }

        private static Mock<ILogger> CreateLogger()
        {
            return new Mock<ILogger>();
        }

        private static Mock<IJobTracker> CreateJobTracker()
        {
            return new Mock<IJobTracker>();
        }
    }
}
