using CalculateFunding.Common.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class UserBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;

        public UserBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public UserBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public Reference Build()
        {
            return new Reference(_id ?? NewRandomString(), 
                _name ?? NewRandomString());
        }
    }
}