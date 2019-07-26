using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Api.External.UnitTests.Version3
{
    [TestClass]
    public class FundingServiceTests
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

            FundingService service = CreateService(searchRepository: searchRepository);

            //Act
            IActionResult result = await service.GetFundingByFundingResultId(fundingResultId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetFundingByFundingResultId_GivenResultFound_ReturnsOkObjectResult()
        {
            //Arrange
            string resultId = "12345";

            PublishedFundingIndex publishedFundingIndex = CreatePublishedFundingResult();

            ISearchRepository<PublishedFundingIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(resultId))
                .Returns(publishedFundingIndex);

            IBlobClient blobClient = CreateBlobClient();

            ICloudBlob cloudBlob = Substitute.For<ICloudBlob>();

            cloudBlob
                .Exists()
                .Returns(true);

            blobClient
                .GetBlockBlobReference(Arg.Is("subfolder/PE.json"))
                .Returns(cloudBlob);

            string publishedFunding = CreateJsonFile("CalculateFunding.Api.External.UnitTests.Resources.PE.json");

            byte[] byteArray = Encoding.UTF8.GetBytes(publishedFunding);

            MemoryStream stream = new MemoryStream(byteArray);

            blobClient
                .DownloadToStreamAsync(Arg.Is(cloudBlob))
                .Returns(stream);

            FundingService service = CreateService(blobClient: blobClient, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.GetFundingByFundingResultId(resultId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult contentResult = result as OkObjectResult;

            JObject content = JObject.Parse(contentResult.Value.ToString());

            content.TryGetValue("schemaVersion", out JToken token);
            ((JValue)token).Value<string>().Should().Be("1.0");
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

        private static FundingService CreateService(IBlobClient blobClient = null, ISearchRepository<PublishedFundingIndex> searchRepository = null, ILogger logger = null)
        {
            return new FundingService(blobClient ?? CreateBlobClient(), searchRepository ?? CreateSearchRepository(), GenerateTestPolicies(), logger ?? CreateLogger());
        }

        private static ISearchRepository<PublishedFundingIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<PublishedFundingIndex>>();
        }

        private static IBlobClient CreateBlobClient()
        {
            return Substitute.For<IBlobClient>();
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
