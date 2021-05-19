using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Converter;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Datasets.Services.Converter
{
    
    
    [TestClass]
    public class DatasetCloneBuilderFactoryTests
    {
        private IDatasetsResiliencePolicies _resilience;
        private Mock<IBlobClient> _blobs;
        private Mock<IExcelDatasetReader> _reader;
        private Mock<IExcelDatasetWriter> _writer;
        private Mock<IDatasetRepository> _datasets;
        private Mock<IDatasetIndexer> _indexer;

        private DatasetCloneBuilderFactory _cloneBuilderFactory;

        [TestInitialize]
        public void SetUp()
        {
            _resilience = new DatasetsResiliencePolicies
            {
                BlobClient = Policy.NoOpAsync(),
                DatasetRepository = Policy.NoOpAsync()
            };
            
            _blobs = new Mock<IBlobClient>();
            _reader = new Mock<IExcelDatasetReader>();
            _writer = new Mock<IExcelDatasetWriter>();
            _datasets = new Mock<IDatasetRepository>();
            _indexer = new Mock<IDatasetIndexer>();

            _cloneBuilderFactory = new DatasetCloneBuilderFactory(_blobs.Object,
                _datasets.Object,
                _reader.Object,
                _writer.Object,
                _indexer.Object,
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

            datasetCloneBuilder
                .Reader
                .Should()
                .BeSameAs(_reader.Object);
            
            datasetCloneBuilder
                .Writer
                .Should()
                .BeSameAs(_writer.Object);
            
            datasetCloneBuilder
                .Datasets
                .Should()
                .BeSameAs(_datasets.Object);
            
            datasetCloneBuilder
                .DatasetsResilience
                .Should()
                .BeSameAs(_resilience.DatasetRepository);
        }

        private IDatasetCloneBuilder WhenThenDatasetCloneBuilderIsCreated()
            => _cloneBuilderFactory.CreateCloneBuilder();
    }
}