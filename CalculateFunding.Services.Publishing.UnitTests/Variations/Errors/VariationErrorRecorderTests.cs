using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Publishing.Variations.Errors;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Errors
{
    [TestClass]
    public class VariationErrorRecorderTests
    {
        private Mock<IBlobClient> _blobClient;
        private VariationErrorRecorder _errorRecorder;

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = new Mock<IBlobClient>();
            
            _errorRecorder = new VariationErrorRecorder(new ResiliencePolicies
            {
                BlobClient = Policy.NoOpAsync()
            }, _blobClient.Object);
        }

        [TestMethod]
        public async Task SerialisesErrorsIntoCsvAndUploadsToBlobStorage()
        {
            string[] variationErrors = new[]
            {
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString()
            };

            string specificationId = NewRandomString();

            await WhenTheErrorsAreRecorded(variationErrors, specificationId);
            
            string expectedFileContents = variationErrors.Join("/n");
            
            _blobClient.Verify(_ => _.UploadFileAsync(
                $"variationerrors_{specificationId}.csv", 
                expectedFileContents, 
                "publishedproviderversions"),
                Times.Once);
        }

        private async Task WhenTheErrorsAreRecorded(IEnumerable<string> errors, string specificationId)
        {
            await _errorRecorder.RecordVariationErrors(errors, specificationId);
        }

        private string NewRandomString() => new RandomString();
    }
}