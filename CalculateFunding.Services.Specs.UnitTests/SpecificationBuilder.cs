using CalculateFunding.Models.Specs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class SpecificationBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private SpecificationVersion _current;
        bool? _isSelectedForFunding;

        public SpecificationBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public SpecificationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }
        
        public SpecificationBuilder WithIsSelectedForFunding(bool isSelectedForFunding)
        {
            _isSelectedForFunding = isSelectedForFunding;

            return this;
        }

        public SpecificationBuilder WithCurrent(SpecificationVersion current)
        {
            _current = current;

            return this;
        }
        
        public Specification Build()
        {
            return new Specification
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Current = _current,
                IsSelectedForFunding = _isSelectedForFunding.GetValueOrDefault(NewRandomFlag())
            };
        }
    }
}