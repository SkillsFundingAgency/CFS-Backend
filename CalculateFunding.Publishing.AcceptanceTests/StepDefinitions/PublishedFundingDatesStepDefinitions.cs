using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class PublishedFundingDatesStepDefinitions
    {
        private readonly IPublishingDatesStepContext _publishingDatesStepContext;
        private ICurrentSpecificationStepContext _currentSpecificationStepContext;

        public PublishedFundingDatesStepDefinitions(
            IPublishingDatesStepContext publishingDatesStepContext,
            ICurrentSpecificationStepContext currentSpecificationStepContext)
        {
            _publishingDatesStepContext = publishingDatesStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
        }

        [Given(@"the publishing dates for the specifcation are set as following")]
        public void GivenThePublishingDatesForTheSpecifcationAreSetAsFollowing(Table table)
        {
            PublishedFundingDates publishedFundingDates = table.CreateInstance<PublishedFundingDates>();

            _publishingDatesStepContext.EmulatedService.SetDatesForSpecification(_currentSpecificationStepContext.SpecificationId, publishedFundingDates);
        }
    }
}
