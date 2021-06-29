using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Undo.Repositories;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo.Repositories
{
    [TestClass]
    public class PublishedFundingUndoBlobStoreRepositoryTests : PublishedFundingUndoTestBase
    {
        private PublishedFundingUndoBlobStoreRepository _repository;
        private Mock<IBlobClient> _blobClient;
        private Mock<ICloudBlob> _cloudBlob;

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = new Mock<IBlobClient>();
            _cloudBlob = new Mock<ICloudBlob>();

            _repository = new PublishedFundingUndoBlobStoreRepository(_blobClient.Object,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync()
                },
                Logger.None);
        }

        [TestMethod]
        public async Task RemovePublishedProviderVersionBlobRemovesBlobDocumentWithSuppliedProviderFundingIdFromVersionsCollection()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion();

            GivenTheCloudBlobIsForBlobNameAndContainer($"{publishedProviderVersion.FundingId}.json", "publishedproviderversions");

            await WhenThePublishedProviderDocumentIsRemoved(publishedProviderVersion);

            ThenTheBlobWasDeleted();
        }

        [TestMethod]
        public async Task RemovePublishedFundingVersionBlobRemovesBlobDocumentWithSuppliedProviderFundingIdFromVersionsCollection()
        {
            PublishedFundingVersion publishedFundingVersion = NewPublishedFundingVersion();

            GivenTheCloudBlobIsForBlobNameAndContainer($"{publishedFundingVersion.FundingId}.json", "publishedfunding");

            await WhenThePublishedFundingDocumentIsRemoved(publishedFundingVersion);

            ThenTheBlobWasDeleted();
        }

        private async Task WhenThePublishedProviderDocumentIsRemoved(PublishedProviderVersion publishedProviderVersion)
        {
            await _repository.RemovePublishedProviderVersionBlob(publishedProviderVersion);
        }

        private async Task WhenThePublishedFundingDocumentIsRemoved(PublishedFundingVersion publishedFundingVersion)
        {
            await _repository.RemovePublishedFundingVersionBlob(publishedFundingVersion);
        }

        private void GivenTheCloudBlobIsForBlobNameAndContainer(string name, string container)
        {
            _blobClient.Setup(_ => _.GetBlobReferenceFromServerAsync(name, container))
                .ReturnsAsync(_cloudBlob.Object);
        }

        private void ThenTheBlobWasDeleted()
        {
            _cloudBlob.Verify(_ => _.DeleteIfExistsAsync(),
                Times.Once);
        }
    }
}