using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Publishing.Batches;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Batches
{
    [TestClass]
    public class BatchUploadReaderFactoryTests
    {
        private Mock<IBlobClient> _blobClient;
        
        private BatchUploadReaderFactory _factory;
        

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = new Mock<IBlobClient>();
            
            _factory = new BatchUploadReaderFactory(_blobClient.Object,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync()
                });
        }


        [TestMethod]
        public void CreatesBatchUploadReaders()
        {
            _factory.CreateBatchUploadReader()
                .Should()
                .BeOfType<BatchUploadReader>();
        }
        
    }
}