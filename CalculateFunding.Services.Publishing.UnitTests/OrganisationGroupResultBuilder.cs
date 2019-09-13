using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class OrganisationGroupResultBuilder : TestEntityBuilder
    {
        private OrganisationGroupTypeClassification _groupTypeClassification;
        private OrganisationGroupTypeCode _groupTypeCode;
        private OrganisationGroupTypeIdentifier _groupTypeIdentifier;
        private string _identifierValue;
        private IEnumerable<Common.ApiClient.Providers.Models.Provider> _providers;
        private IEnumerable<OrganisationIdentifier> _identifiers;

        public OrganisationGroupResultBuilder WithGroupTypeClassification(OrganisationGroupTypeClassification groupTypeClassification)
        {
            _groupTypeClassification = groupTypeClassification;

            return this;
        }

        public OrganisationGroupResultBuilder WithGroupTypeCode(OrganisationGroupTypeCode groupTypeCode)
        {
            _groupTypeCode = groupTypeCode;

            return this;
        }

        public OrganisationGroupResultBuilder WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier groupTypeIdentifier)
        {
            _groupTypeIdentifier = groupTypeIdentifier;

            return this;
        }

        public OrganisationGroupResultBuilder WithIdentifierValue(string identifierValue)
        {
            _identifierValue = identifierValue;

            return this;
        }

        public OrganisationGroupResultBuilder WithIdentifiers(IEnumerable<OrganisationIdentifier> identifiers)
        {
            _identifiers = identifiers;

            return this;
        }

        public OrganisationGroupResultBuilder WithProviders(IEnumerable<Common.ApiClient.Providers.Models.Provider> providers)
        {
            _providers = providers;

            return this;
        }

        public OrganisationGroupResult Build()
        {
            return new OrganisationGroupResult
            {
                GroupTypeClassification = _groupTypeClassification,
                GroupTypeCode = _groupTypeCode,
                GroupTypeIdentifier = _groupTypeIdentifier,
                IdentifierValue = _identifierValue,
                Identifiers = _identifiers,
                Providers = _providers
            };
        }
    }
}