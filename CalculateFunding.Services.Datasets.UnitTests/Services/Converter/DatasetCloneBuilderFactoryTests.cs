using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Datasets.Converter;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DatasetCloneBuilderFactoryTests
    {
        private IDatasetsResiliencePolicies _resilience;
        private Mock<IBlobClient> _blobs;

        private DatasetCloneBuilderFactory _cloneBuilderFactory;

        [TestInitialize]
        public void SetUp()
        {
            _resilience = new DatasetsResiliencePolicies
            {
                BlobClient = Policy.NoOpAsync()
            };
            
            _blobs = new Mock<IBlobClient>();

            _cloneBuilderFactory = new DatasetCloneBuilderFactory(_blobs.Object,
                _resilience);
        }

        [TestMethod]
        public void CreatesDatasetCloneBuilderInstanceWithSharedDependencies()
        {
            IDatasetCloneBuilder datasetCloneBuilder = WhenThenDatasetCloneBuilderIsCreated();

            datasetCloneBuilder
                .Should()
                .BeOfType<DatasetCloneBuilder>();

            datasetCloneBuilder
                .Blobs
                .Should()
                .BeSameAs(_blobs.Object);

            datasetCloneBuilder
                .BlobsResilience
                .Should()
                .BeSameAs(_resilience.BlobClient);
        }

        private IDatasetCloneBuilder WhenThenDatasetCloneBuilderIsCreated()
            => _cloneBuilderFactory.CreateCloneBuilder();
    }
}