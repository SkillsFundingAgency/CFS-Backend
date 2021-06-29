using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DefinitionSpecificationRelationshipBuilder : TestEntityBuilder
    {
        private DefinitionSpecificationRelationshipVersion _current;
        private string _id;
        private string _name;

        public DefinitionSpecificationRelationshipBuilder WithCurrent(DefinitionSpecificationRelationshipVersion current)
        {
            _current = current;

            return this;
        }

        public DefinitionSpecificationRelationshipBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public DefinitionSpecificationRelationshipBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public DefinitionSpecificationRelationship Build()
        {
            return new DefinitionSpecificationRelationship
            {
                Current = _current,
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString()
            };
        }
    }
}