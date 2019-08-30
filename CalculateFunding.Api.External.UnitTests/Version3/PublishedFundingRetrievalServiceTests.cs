using System.IO;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Core.Caching.FileSystem;
using FluentAssertions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Api.External.UnitTests.Version3
{
    [TestClass]
    public class PublishedFundingRetrievalServiceTests
    {
        [TestMethod]
        [DataRow("Garden")]
        [DataRow("Woodland")]
        public async Task GetFundingFeedDocument_BlobDoesntExist_LogsAndReturnsNull(string documentPath)
        {
            string absoluteDocumentPathUrl = $"https://cfs/base/{documentPath}";

            ICloudBlob cloudBlob = CreateBlob();
            cloudBlob
                .Exists()
                .Returns(false);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlockBlobReference(Arg.Any<string>())
                .Returns(cloudBlob);

            ILogger logger = CreateLogger();

            PublishedFundingRetrievalService service = CreatePublishedFundingRetrievalService(
                blobClient: blobClient,
                logger: logger);

            string result = await service.GetFundingFeedDocument(absoluteDocumentPathUrl);

            result
                .Should()
                .BeNull();

            blobClient
                .Received(1)
                .GetBlockBlobReference(documentPath);

            cloudBlob
                .Received(1)
                .Exists(null, null);

            logger
                .Received(1)
                .Error($"Failed to find blob with path: {documentPath}");
        }

        [TestMethod]
        public async Task GetFundingFeedDocument_NoFundingStream_LogsAndReturnsNull()
        {
            string documentPath = "uri";
            string absoluteDocumentPathUrl = $"https://cfs/base/{documentPath}";

            ICloudBlob cloudBlob = CreateBlob();
            cloudBlob
                .Exists()
                .Returns(true);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlockBlobReference(Arg.Any<string>())
                .Returns(cloudBlob);

            blobClient
                .DownloadToStreamAsync(cloudBlob)
                .Returns(new MemoryStream());

            ILogger logger = CreateLogger();

            PublishedFundingRetrievalService service = CreatePublishedFundingRetrievalService(
                blobClient: blobClient,
                logger: logger);

            string result = await service.GetFundingFeedDocument(absoluteDocumentPathUrl);

            result
                .Should()
                .BeNull();

            blobClient
                .Received(1)
                .GetBlockBlobReference(documentPath);

            cloudBlob
                .Received(1)
                .Exists(null, null);

            await blobClient
                .Received(1)
                .DownloadToStreamAsync(cloudBlob);

            logger
                .Received(1)
                .Error($"Invalid blob returned: {documentPath}");
        }

        [TestMethod]
        [DataRow("just a string")]
        [DataRow("oh little old me")]
        [DataRow("they won't let me drive late at night")]
        public async Task GetFundingFeedDocument_WhenReturnsFromBlobStorage_ReturnsOK(string template)
        {
            //Arrange
            string documentPath = "cromulent.json";
            string uri = $"https://cfs/test/{documentPath}";

            Stream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(template));

            ILogger logger = CreateLogger();
            ICloudBlob cloudBlob = CreateBlob();
            IFileSystemCache fileSystemCache = CreateFileSystemCache();

            cloudBlob
                .Exists()
                .Returns(true);
            cloudBlob
                .Exists(Arg.Any<BlobRequestOptions>(), Arg.Any<OperationContext>())
                .Returns(true);

            IBlobClient blobClient = CreateBlobClient();

            blobClient
                 .GetBlockBlobReference(Arg.Is(documentPath))
                 .Returns(cloudBlob);

            blobClient
                  .DownloadToStreamAsync(Arg.Is(cloudBlob))
                  .Returns(memoryStream);

            PublishedFundingRetrievalService service = CreatePublishedFundingRetrievalService(
                blobClient: blobClient,
                logger: logger,
                fileSystemCache: fileSystemCache);

            //Act
            string result = await service.GetFundingFeedDocument(uri);

            //Assert
            result
                .Should()
                .Be(template);

            blobClient
                .Received(1)
                .GetBlockBlobReference(documentPath);

            cloudBlob
                .Received(1)
                .Exists();

            logger
                .Received(0)
                .Error(Arg.Any<string>());

            await blobClient
                .Received(1)
                .DownloadToStreamAsync(cloudBlob);

            fileSystemCache
                .Received(0)
                .Get(Arg.Any<FundingFileSystemCacheKey>());

            string documentName = Path.GetFileNameWithoutExtension(documentPath);
            
            fileSystemCache
                .Received(1)
                .Add(Arg.Is<FundingFileSystemCacheKey>(
                    _ => _.Key == documentName), 
                    memoryStream);
        }

        [TestMethod]
        [DataRow("https://cfs/test/2345", "2345")]
        [DataRow("https://www.education.gov.uk/cfs/embiggen", "embiggen")]
        [DataRow("https://strgt1dvprovcfs.blob.core.windows.net/publishedfunding/subfolder/PES-AY-1920-Payment-LocalAuthority-12345678-1_0.json", 
            "subfolder/PES-AY-1920-Payment-LocalAuthority-12345678-1_0.json")]
        public void ParseDocumentPathRelativeToBlobContainerFromFullUrl_ParsesAsExpected(string input, string output)
        {
            //Arrange
            PublishedFundingRetrievalService service = CreatePublishedFundingRetrievalService();

            //Act
            string result = service.ParseDocumentPathRelativeToBlobContainerFromFullUrl(input);

            //Assert
            result.Should().Be(output);
        }

        private static PublishedFundingRetrievalService CreatePublishedFundingRetrievalService(
            IBlobClient blobClient = null,
            ILogger logger = null,
            IFileSystemCache fileSystemCache = null)
        {
            return new PublishedFundingRetrievalService(
                blobClient ?? CreateBlobClient(),
                PublishingResilienceTestHelper.GenerateTestPolicies(),
                fileSystemCache ?? CreateFileSystemCache(),
                logger ?? CreateLogger());
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IBlobClient CreateBlobClient()
        {
            return Substitute.For<IBlobClient>();
        }

        private static ICloudBlob CreateBlob()
        {
            return Substitute.For<ICloudBlob>();
        }

        private static IFileSystemCache CreateFileSystemCache()
        {
            return Substitute.For<IFileSystemCache>();
        }
    }
}
