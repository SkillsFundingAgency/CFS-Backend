using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class BatchProfilingOptionsTests
    {
        private Mock<IConfiguration> _configuration;

        private BatchProfilingOptions _options;

        [TestInitialize]
        public void SetUp()
        {
            _configuration = new Mock<IConfiguration>();

            _options = new BatchProfilingOptions(_configuration.Object);
        }

        [TestMethod]
        [DataRow("1", "BatchSize", 1)]
        [DataRow("99", "BatchSize", 99)]
        [DataRow("1", "NotBatchSizeKey", 50)]
        public void ReadsConfiguredBatchSizeLiteral(string literal,
            string key,
            int expectedBatchSize)
        {
            GivenTheConfigurationValue(key, literal);
            
            _options.BatchSize
                .Should()
                .Be(expectedBatchSize);
        }
        
        [TestMethod]
        [DataRow("20", "ConsumerCount", 20)]
        [DataRow("50", "ConsumerCount", 50)]
        [DataRow("1", "NotConsumerCount", 10)]
        public void ReadsConfiguredConsumerCountLiteral(string literal,
            string key,
            int expectedBatchSize)
        {
            GivenTheConfigurationValue(key, literal);
            
            _options.ConsumerCount
                .Should()
                .Be(expectedBatchSize);
        }

        private void GivenTheConfigurationValue(string key,
            string literal)
            => _configuration.Setup(_ => _[$"batchProfilingOptions:{key}"])
                .Returns(literal);

    }
}