using CalculateFunding.Common.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class ReferenceBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;

        public ReferenceBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public ReferenceBuilder WithName(string name)
        {
            _name = name;

            return this;
        }
        
        public Reference Build()
        {
            return new Reference(_id ?? NewRandomString(), _name ?? NewRandomString());
        }
    }
}