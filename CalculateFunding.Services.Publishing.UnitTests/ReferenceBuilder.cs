using CalculateFunding.Common.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ReferenceBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private bool _withNoId;

        public ReferenceBuilder WithNoId()
        {
            _withNoId = true;

            return this;
        }

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
            return new Reference(_withNoId ? null : _id ?? NewRandomString(),
                _name ?? NewRandomString());
        }
    }
}