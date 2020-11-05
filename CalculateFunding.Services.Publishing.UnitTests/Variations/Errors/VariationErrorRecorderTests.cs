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
            string jobId = NewRandomString();

            await WhenTheErrorsAreRecorded(variationErrors, specificationId, jobId);
            
            string expectedFileContents = variationErrors.Join("\n");
            
            _blobClient.Verify(_ => _.UploadFileAsync(
                $"{specificationId}/variationerrors_{jobId}.csv", 
                expectedFileContents,
                "variationerrors"),
                Times.Once);
        }

        private async Task WhenTheErrorsAreRecorded(IEnumerable<string> errors, string specificationId, string jobId)
        {
            await _errorRecorder.RecordVariationErrors(errors, specificationId, jobId);
        }

        private string NewRandomString() => new RandomString();
    }
}