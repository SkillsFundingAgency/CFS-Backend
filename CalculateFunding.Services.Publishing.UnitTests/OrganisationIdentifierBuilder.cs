using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class OrganisationIdentifierBuilder : TestEntityBuilder
    {
        private OrganisationGroupTypeIdentifier _type;
        private string _value;

        public OrganisationIdentifierBuilder WithType(OrganisationGroupTypeIdentifier type)
        {
            _type = type;
            return this;
        }

        public OrganisationIdentifierBuilder WithValue(string value)
        {
            _value = value;
            return this;
        }

        public OrganisationIdentifier Build()
        {
            return new OrganisationIdentifier
            {
                Type = _type,
                Value = _value
            };
        }
    }
}