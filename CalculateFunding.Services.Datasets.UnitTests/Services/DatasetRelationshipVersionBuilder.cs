using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DatasetRelationshipVersionBuilder : TestEntityBuilder
    {
        private string _id;
        private int? _version;

        public DatasetRelationshipVersionBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public DatasetRelationshipVersionBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public DatasetRelationshipVersion Build()
        {
            return new DatasetRelationshipVersion
            {
                Id = _id ?? NewRandomString(),
                Version = _version.GetValueOrDefault(NewRandomNumberBetween(1, 99)),
            };
        }
    }
}