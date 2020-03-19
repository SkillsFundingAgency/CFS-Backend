using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedFundingOrganisationGroupingBuilder : TestEntityBuilder
    {
        private OrganisationGroupResult _organisationGroupResult;
        private PublishedFunding _publishedFunding;

        public PublishedFundingOrganisationGroupingBuilder WithPublishedFunding(PublishedFunding publishedFunding)
        {
            _publishedFunding = publishedFunding;

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
                PublishedFunding = _publishedFunding
            };
        }

    }
}
