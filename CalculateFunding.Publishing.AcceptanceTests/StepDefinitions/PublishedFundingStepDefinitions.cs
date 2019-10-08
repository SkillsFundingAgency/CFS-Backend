using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Models;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class PublishedFundingStepDefinitions
    {
        private readonly IPublishedFundingRepositoryStepContext _publishedFundingRepositoryStepContext;
        private readonly IPublishedFundingResultStepContext _publishedFundingResultStepContext;

        public PublishedFundingStepDefinitions(IPublishedFundingRepositoryStepContext publishedFundingRepositoryStepContext,
            IPublishedFundingResultStepContext publishedFundingResultStepContext)
        {
            Guard.ArgumentNotNull(publishedFundingRepositoryStepContext, nameof(publishedFundingRepositoryStepContext));
            Guard.ArgumentNotNull(publishedFundingResultStepContext, nameof(publishedFundingResultStepContext));

            _publishedFundingRepositoryStepContext = publishedFundingRepositoryStepContext;
            _publishedFundingResultStepContext = publishedFundingResultStepContext;
        }

        [Then(@"the following published funding is produced")]
        public async Task ThenTheFollowingPublishedFundingIsProduced(Table table)
        {
            PublishedFundingLookupModel lookupModel = table.CreateInstance<PublishedFundingLookupModel>();

            _publishedFundingRepositoryStepContext.Repo.Should().NotBeNull();

            string fundingId = $"funding-{lookupModel.FundingStreamId}-{lookupModel.FundingPeriodId}-{lookupModel.GroupingReason}-{lookupModel.OrganisationGroupTypeCode}-{lookupModel.OrganisationGroupIdentifierValue}";

            CalculateFunding.Models.Publishing.PublishedFunding publishedFunding = await _publishedFundingRepositoryStepContext.Repo
                .GetPublishedFundingById(fundingId, "partitionNotUesd");

            publishedFunding
                .Should()
                .NotBeNull("Published funding not found for ID '{0}'", fundingId);

            _publishedFundingResultStepContext.CurrentPublishedFunding = publishedFunding;
        }

        [Then(@"the published funding contains the following published provider ids")]
        public void ThenThePublishedFundingContainsTheFollowingPublishedProviderIds(Table table)
        {
            PublishedFunding publishedFunding = _publishedFundingResultStepContext.CurrentPublishedFunding;

            publishedFunding.Should()
                .NotBeNull();

            List<string> expectedPublishedProviderIds = new List<string>();

            for (int i = 0; i < table.Rows.Count; i++)
            {
                expectedPublishedProviderIds.Add(table.Rows[i][0]);
            }

            publishedFunding
                .Current
                .ProviderFundings
                .Should()
                .BeEquivalentTo(expectedPublishedProviderIds);
        }


        [Then(@"the total funding is '(.*)'")]
        public void ThenTheTotalFundingIs(decimal expectedTotalFunding)
        {
            PublishedFunding publishedFunding = _publishedFundingResultStepContext.CurrentPublishedFunding;

            publishedFunding.Should()
                .NotBeNull();

            publishedFunding
                .Current
                .TotalFunding
                .Should()
                .Be(expectedTotalFunding);
        }

        [Then(@"the published funding contains a distribution period in funding line '(.*)' with id of '(.*)' has the value of '(.*)'")]
        public void ThenTheDistributionPeriodInFundingLineWithIdOfHasTheValueOf(string fundingLineCode, string distributionPeriodId, decimal expectedValue)
        {
            PublishedFunding publishedFunding = _publishedFundingResultStepContext.CurrentPublishedFunding;

            publishedFunding.Should()
                .NotBeNull();

            var fundingLine = publishedFunding.Current.FundingLines.SingleOrDefault(c => c.FundingLineCode == fundingLineCode);

            fundingLine
                .Should()
                .NotBeNull("funding line not found");

            var distributionPeriod = fundingLine.DistributionPeriods.SingleOrDefault(c => c.DistributionPeriodId == distributionPeriodId);
            distributionPeriod
                .Should()
                .NotBeNull("distribution period could not be found");

            distributionPeriod
                .Value
                .Should()
                .Be(expectedValue);
        }


    }
}
