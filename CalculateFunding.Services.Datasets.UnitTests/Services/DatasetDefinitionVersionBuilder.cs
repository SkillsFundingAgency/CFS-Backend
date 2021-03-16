using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DatasetDefinitionVersionBuilder : TestEntityBuilder
    {
        private int? _version;
        private Reference _reference;

        public DatasetDefinitionVersionBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public DatasetDefinitionVersionBuilder WithReference(Reference reference)
        {
            _reference = reference;

            return this;
        }
        
        public DatasetDefinitionVersion Build()
        {
            return  new DatasetDefinitionVersion
            {
                Version = _version,
                Id = _reference?.Id,
                Name = _reference?.Name
            };
        }
    }
}