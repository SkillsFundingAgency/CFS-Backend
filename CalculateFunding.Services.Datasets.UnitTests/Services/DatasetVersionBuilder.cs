using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DatasetVersionBuilder : TestEntityBuilder
    {
        private string _blobName;
        private int? _version;

        public DatasetVersionBuilder WithBlobName(string blobName)
        {
            _blobName = blobName;

            return this;
        }

        public DatasetVersionBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }
        
        public DatasetVersion Build()
        {
            return new DatasetVersion
            {
                BlobName = _blobName ?? NewRandomString(),
                Version = _version.GetValueOrDefault(NewRandomNumberBetween(1, 99))
            };
        }
    }
}