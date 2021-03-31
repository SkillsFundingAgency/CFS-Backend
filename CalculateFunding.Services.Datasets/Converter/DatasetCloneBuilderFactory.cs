using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class DatasetCloneBuilderFactory : IDatasetCloneBuilderFactory
    {
        private readonly IBlobClient _blobs;
        private readonly IExcelDatasetReader _reader;
        private readonly IExcelDatasetWriter _writer;
        private readonly IDatasetRepository _datasets;
        private readonly IDatasetIndexer _indexer;
        private readonly IDatasetsResiliencePolicies _resiliencePolicies;

        public DatasetCloneBuilderFactory(IBlobClient blobs,
            IDatasetRepository datasets,
            IExcelDatasetReader reader,
            IExcelDatasetWriter writer,
            IDatasetIndexer indexer,
            IDatasetsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(reader, nameof(reader));
            Guard.ArgumentNotNull(writer, nameof(writer));
            Guard.ArgumentNotNull(blobs, nameof(blobs));
            Guard.ArgumentNotNull(datasets, nameof(datasets));
            Guard.ArgumentNotNull(indexer, nameof(indexer));
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, nameof(resiliencePolicies.BlobClient));
            Guard.ArgumentNotNull(resiliencePolicies.DatasetRepository, nameof(resiliencePolicies.DatasetRepository));

            _blobs = blobs;
            _resiliencePolicies = resiliencePolicies;
            _indexer = indexer;
            _datasets = datasets;
            _writer = writer;
            _reader = reader;
        }

        public IDatasetCloneBuilder CreateCloneBuilder()
        {
            return new DatasetCloneBuilder(_blobs,
                _datasets,
                _reader,
                _writer,
                _indexer,
                _resiliencePolicies);
        }
    }
}