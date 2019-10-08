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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
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

            ILogger logger = CreateLogger();

            IPublishedProviderVersioningService providerVersioningService = CreateVersioningService();
            IJobTracker jobTracker = CreateJobTracker();

            providerVersioningService
                .AssemblePublishedProviderCreateVersionRequests(Arg.Any<IEnumerable<PublishedProvider>>(), Arg.Is(author),
                    Arg.Is(PublishedProviderStatus.Approved))
                .Returns(publishedProviderCreateVersionRequests);

            providerVersioningService
                .CreateVersions(Arg.Is(publishedProviderCreateVersionRequests))
                .Returns(publishedProviders);

            providerVersioningService
                .CreateVersions(Arg.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(_
                    => _.SequenceEqual(publishedProviderCreateVersionRequests.Take(200))))
                .Returns(publishedProviders.Take(200));

            providerVersioningService
                .CreateVersions(Arg.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(_
                    => _.SequenceEqual(publishedProviderCreateVersionRequests.Skip(200).Take(200))))
                .Returns(publishedProviders.Skip(200).Take(200));

            providerVersioningService
                .CreateVersions(Arg.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(_
                    => _.SequenceEqual(publishedProviderCreateVersionRequests.Skip(400).Take(200))))
                .Returns(publishedProviders.Skip(400).Take(200));

            providerVersioningService
                .CreateVersions(Arg.Is<IEnumerable<PublishedProviderCreateVersionRequest>>(_
                    => _.SequenceEqual(publishedProviderCreateVersionRequests.Skip(600).Take(200))))
                .Returns(publishedProviders.Skip(600).Take(200));


            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService, logger, jobTracker);


            //Act
            await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved, jobId);

            //Assert
            // TODO - fix due to further batching because of optmisation
            //await providerVersioningService
            //    .Received(1)
            //    .SaveVersions(Arg.Is<IEnumerable<PublishedProvider>>(_ =>
            //        _.SequenceEqual(publishedProviders.Take(200))));

            //await providerVersioningService
            //    .Received(1)
            //    .SaveVersions(Arg.Is<IEnumerable<PublishedProvider>>(_ =>
            //        _.SequenceEqual(publishedProviders.Skip(200).Take(200))));

            //await providerVersioningService
            //    .Received(1)
            //    .SaveVersions(Arg.Is<IEnumerable<PublishedProvider>>(_ =>
            //        _.SequenceEqual(publishedProviders.Skip(400).Take(200))));

            //await providerVersioningService
            //    .Received(1)
            //    .SaveVersions(Arg.Is<IEnumerable<PublishedProvider>>(_ =>
            //        _.SequenceEqual(publishedProviders.Skip(600).Take(200))));

            await jobTracker
                .Received(1)
                .NotifyProgress(200, jobId);

            await jobTracker
                .Received(1)
                .NotifyProgress(400, jobId);

            await jobTracker
                .Received(1)
                .NotifyProgress(600, jobId);

            await jobTracker
                .Received(1)
                .NotifyProgress(605, jobId);
        }

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

            string errorMessage = "No published providers were assembled for updating.";

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
                .AssemblePublishedProviderCreateVersionRequests(Arg.Any<IEnumerable<PublishedProvider>>(), Arg.Is(author), Arg.Is(PublishedProviderStatus.Approved))
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

            IPublishedFundingRepository publishedFundingRepository = CreatePublishedFundingRepository();

            providerVersioningService
                .AssemblePublishedProviderCreateVersionRequests(Arg.Any<IEnumerable<PublishedProvider>>(), Arg.Is(author), Arg.Is(PublishedProviderStatus.Approved))
                .Returns(publishedProviderCreateVersionRequests);

            providerVersioningService
                .CreateVersions(Arg.Is(publishedProviderCreateVersionRequests))
                .Returns(publishedProviders);

            publishedFundingRepository.UpsertPublishedProviders(Arg.Is(publishedProviders))
                .Returns(new[] { HttpStatusCode.OK });

            providerVersioningService
                .When(x => x.SaveVersions(Arg.Any<IEnumerable<PublishedProvider>>()))
                .Do(x => { throw new Exception(); });

            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService, logger: logger, publishedFundingRepository: publishedFundingRepository);

            string errorMessage = $"Failed to save versions when updating status:' {PublishedProviderStatus.Approved}' on published providers.";

            //Assert
            Func<Task> test = async () => await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved);

            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .InnerException
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
                .AssemblePublishedProviderCreateVersionRequests(Arg.Any<IEnumerable<PublishedProvider>>(), Arg.Is(author), Arg.Is(PublishedProviderStatus.Approved))
                .Returns(publishedProviderCreateVersionRequests);

            providerVersioningService
                .CreateVersions(Arg.Is(publishedProviderCreateVersionRequests))
                .Returns(publishedProviders);

            PublishedProviderStatusUpdateService publishedProviderStatusUpdateService =
                CreatePublishedProviderStatusUpdateService(providerVersioningService, logger: logger);

            string errorMessage = $"Failed to save versions when updating status:' {PublishedProviderStatus.Approved}' on published providers.";

            //Assert
            await publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved);

            await
                providerVersioningService
                    .DidNotReceive()
                    .SaveVersions(Arg.Any<IEnumerable<PublishedProvider>>());
        }

        private static PublishedProviderStatusUpdateService CreatePublishedProviderStatusUpdateService(
            IPublishedProviderVersioningService publishedProviderVersioningService = null,
            ILogger logger = null,
            IJobTracker jobTracker = null,
            IPublishedFundingRepository publishedFundingRepository = null)
        {
            return new PublishedProviderStatusUpdateService(
                    publishedProviderVersioningService ?? CreateVersioningService(),
                    publishedFundingRepository ?? CreatePublishedFundingRepository(),
                    jobTracker ?? CreateJobTracker(),
                    logger ?? CreateLogger(),
                    new PublishedProviderStatusUpdateSettings()
                );
        }


        private static IPublishedProviderVersioningService CreateVersioningService()
        {
            return Substitute.For<IPublishedProviderVersioningService>();
        }

        private static IPublishedFundingRepository CreatePublishedFundingRepository()
        {
            return Substitute.For<IPublishedFundingRepository>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IJobTracker CreateJobTracker()
        {
            return Substitute.For<IJobTracker>();
        }
    }
}
