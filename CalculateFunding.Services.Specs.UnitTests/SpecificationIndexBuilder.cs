using CalculateFunding.Models.Specs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class SpecificationIndexBuilder : TestEntityBuilder
    {
        private string _id;

        public SpecificationIndexBuilder WithId(string id)
        {
            _id = id;

            return this;
        }
        
        public SpecificationIndex Build()
        {
            return new SpecificationIndex
            {
                Id = _id ?? NewRandomString()
            };
        }
    }
}