using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DatasetRelationshipVersionBuilder : TestEntityBuilder
    {
        private int? _version;

        public DatasetRelationshipVersionBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public DatasetRelationshipVersion Build()
        {
            return new DatasetRelationshipVersion
            {
                Version = _version.GetValueOrDefault(NewRandomNumberBetween(1, 99))
            };
        }
    }
}