using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Api.External.UnitTests.Version3
{
    [TestClass]
    public class FundingFeedItemByIdServiceTests
    {
        [TestMethod]
        public async Task GetFundingByFundingResultId_GivenNullResultFound_ReturnsNotFound()
        {
            //Arrange
            string fundingResultId = "12345";

            ISearchRepository<PublishedFundingIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(fundingResultId))
                .Returns(CreatePublishedFundingResult());

            FundingFeedItemByIdService service = CreateService(searchRepository: searchRepository);

            //Act
            IActionResult result = await service.GetFundingByFundingResultId(fundingResultId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetFundingByFundingResultId_GivenRepoReturnsNothing_ReturnsNotFoundResult()
        {
            //Arrange
            string resultId = "12345";

            ISearchRepository<PublishedFundingIndex> searchRepository = CreateSearchRepository();

            FundingFeedItemByIdService service = CreateService(
                searchRepository: searchRepository);

            //Act
            IActionResult result = await service.GetFundingByFundingResultId(resultId);

            //Assert
            result
                .Should().BeOfType<NotFoundResult>();

            await searchRepository
                .Received(1)
                .SearchById(resultId);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("              ")]
        public async Task GetFundingByFundingResultId_GivenEmptyResultFound_ReturnsNotFoundResult(string document)
        {
            //Arrange
            string resultId = "12345";
            string documentPath = "Round the ragged rocks the ragged rascal ran";

            PublishedFundingIndex publishedFundingIndex = CreatePublishedFundingResult();
            publishedFundingIndex.DocumentPath = documentPath;

            ISearchRepository<PublishedFundingIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(resultId))
                .Returns(publishedFundingIndex);

            IPublishedFundingRetrievalService publishedFundingRetrievalService = Substitute.For<IPublishedFundingRetrievalService>();
            publishedFundingRetrievalService
                .GetFundingFeedDocument(documentPath)
                .Returns(document);

            FundingFeedItemByIdService service = CreateService(searchRepository: searchRepository,
                publishedFundingRetrievalService: publishedFundingRetrievalService);

            //Act
            IActionResult result = await service.GetFundingByFundingResultId(resultId);

            //Assert
            result
                .Should().BeOfType<NotFoundResult>();

            await searchRepository
                .Received(1)
                .SearchById(resultId);
        }

        [TestMethod]
        public async Task GetFundingByFundingResultId_GivenResultFound_ReturnsContentResult()
        {
            //Arrange
            string resultId = "12345";
            string documentPath = "Round the ragged rocks the ragged rascal ran";
            string fundingDocument = "Now is the time for all good men to come to the aid of the party.";

            PublishedFundingIndex publishedFundingIndex = CreatePublishedFundingResult();
            publishedFundingIndex.DocumentPath = documentPath;

            ISearchRepository<PublishedFundingIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(resultId))
                .Returns(publishedFundingIndex);

            IPublishedFundingRetrievalService publishedFundingRetrievalService = Substitute.For<IPublishedFundingRetrievalService>();
            publishedFundingRetrievalService
                .GetFundingFeedDocument(documentPath)
                .Returns(fundingDocument);

            FundingFeedItemByIdService service = CreateService(searchRepository: searchRepository,
                publishedFundingRetrievalService: publishedFundingRetrievalService);

            //Act
            IActionResult result = await service.GetFundingByFundingResultId(resultId);

            //Assert
            result
                .Should().BeOfType<ContentResult>()
                .Which
                .StatusCode
                .Should().Be((int)HttpStatusCode.OK);

            await searchRepository
                .Received(1)
                .SearchById(resultId);

            await publishedFundingRetrievalService
                .Received(1)
                .GetFundingFeedDocument(documentPath);

            ContentResult contentResult = result as ContentResult;

            contentResult.Content
                .Should().Be(fundingDocument);
            contentResult.ContentType
                .Should().Be("application/json");
        }

        private static string CreateJsonFile(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                StreamReader reader = new StreamReader(stream);

                return reader.ReadToEnd();
            }
        }

        private static PublishedFundingIndex CreatePublishedFundingResult()
        {
            return new PublishedFundingIndex { Id = "12345", FundingStreamId = "PES", FundingPeriodId = "4567", DocumentPath = "https://strgt1dvprovcfs.blob.core.windows.net/publishedfunding/subfolder/PE.json" };
        }

        private static FundingFeedItemByIdService CreateService(
            IPublishedFundingRetrievalService publishedFundingRetrievalService = null,
            ISearchRepository<PublishedFundingIndex> searchRepository = null,
            ILogger logger = null)
        {
            return new FundingFeedItemByIdService(
                publishedFundingRetrievalService ?? CreatePublishedFundingRetrievalService(),
                searchRepository ?? CreateSearchRepository(),
                GenerateTestPolicies(),
                logger ?? CreateLogger());
        }

        private static IPublishedFundingRetrievalService CreatePublishedFundingRetrievalService()
        {
            return Substitute.For<IPublishedFundingRetrievalService>();
        }

        private static ISearchRepository<PublishedFundingIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<PublishedFundingIndex>>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        public static IPublishingResiliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                FundingFeedSearchRepository = Policy.NoOpAsync(),
                PublishedFundingBlobRepository = Policy.NoOpAsync()
            };
        }
    }
}
