using CalculateFunding.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class VersionReferenceBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private int? _version;

        public VersionReferenceBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public VersionReferenceBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public VersionReferenceBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }
            
        public VersionReference Build()
        {
            return new VersionReference(_id ?? NewRandomString(), 
                _name ?? NewRandomString(), 
                _version.GetValueOrDefault(NewRandomNumberBetween(1, 99)));
        }
    }
}