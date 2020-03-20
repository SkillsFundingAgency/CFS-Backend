using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedFundingOrganisationGroupingBuilder : TestEntityBuilder
    {
        private OrganisationGroupResult _organisationGroupResult;
        private IEnumerable<PublishedFundingVersion> _publishedFundingVersions;

        public PublishedFundingOrganisationGroupingBuilder WithPublishedFundingVersions(IEnumerable<PublishedFundingVersion> publishedFundingVersions)
        {
            _publishedFundingVersions = publishedFundingVersions;

            return this;
        }

        public PublishedFundingOrganisationGroupingBuilder WithOrganisationGroupResult(OrganisationGroupResult organisationGroupResult)
        {
            _organisationGroupResult = organisationGroupResult;

            return this;
        }

        public PublishedFundingOrganisationGrouping Build()
        {
            return new PublishedFundingOrganisationGrouping
            {
                OrganisationGroupResult = _organisationGroupResult,
                PublishedFundingVersions = _publishedFundingVersions
            };
        }

    }
}
