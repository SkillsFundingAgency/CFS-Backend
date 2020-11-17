using System;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Calcs.UnitTests
{
    [TestClass]
    public class SourceFileRepositoryTests
    {
        private Mock<IBlobContainerRepository> _blobContainer;
        private Mock<ICloudBlob> _cloudBlob;

        private SourceFileRepository _sourceFileRepository;
        
        [TestInitialize]
        public void SetUp()
        {
            _blobContainer = new Mock<IBlobContainerRepository>();
            _cloudBlob = new Mock<ICloudBlob>();
            
            _sourceFileRepository = new SourceFileRepository(_blobContainer.Object);
        }

        [TestMethod]
        public async Task GetAssemblyETag_FetchesEtagPropertyForSpecificationBlob()
        {
            string specificationId = NewRandomString();
            string expectedETag = NewRandomString();

            GivenTheSpecificationBlob(specificationId);
            AndTheBlobExists(true);
            AndTheEtag(expectedETag);

            string eTag = await WhenEtagRequested(specificationId);

            eTag
                .Should()
                .Be(expectedETag);
        }

        [TestMethod]
        public async Task GetAssemblyETag_ReturnsNullIfBlobDoesntExist()
        {
            string specificationId = NewRandomString();
            string expectedETag = NewRandomString();

            GivenTheSpecificationBlob(specificationId);
            AndTheBlobExists();
            AndTheEtag(expectedETag);

            string eTag = await WhenEtagRequested(specificationId);

            eTag
                .Should()
                .Be(null);
        }

        private void GivenTheSpecificationBlob(string specificationId)
            => _blobContainer.Setup(_ => _.GetBlockBlobReference($"{specificationId}/implementation.dll", null)).Returns(_cloudBlob.Object);

        private void AndTheBlobExists(bool exists = false)
             => _cloudBlob.Setup(_ => _.ExistsAsync()).ReturnsAsync(exists);

        private void AndTheEtag(string etag)
        {
            var cloudBlobProperties = new BlobProperties();
            var property = typeof(BlobProperties).GetProperty("ETag");
            property.SetValue(cloudBlobProperties, etag, BindingFlags.NonPublic, null, null, null);

            _cloudBlob.Setup(_ => _.Properties).Returns(cloudBlobProperties);
        }

        private async Task<string> WhenEtagRequested(string specificationId) => await _sourceFileRepository.GetAssemblyETag(specificationId);

        private string NewRandomString() => new RandomString();

    }
}