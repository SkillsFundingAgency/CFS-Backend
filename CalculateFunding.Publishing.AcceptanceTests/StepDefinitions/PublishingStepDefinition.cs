using System.Threading.Tasks;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class PublishingStepDefinition
    {
        private readonly IPublishFundingStepContext _publishFundingStepContext;
        private readonly CurrentSpecificationStepContext _currentSpecificationStepContext;
        private readonly CurrentJobStepContext _currentJobStepContext;
        private readonly CurrentUserStepContext _currentUserStepContext;

        public PublishingStepDefinition(IPublishFundingStepContext publishFundingStepContext,
            CurrentSpecificationStepContext currentSpecificationStepContext,
            CurrentJobStepContext currentJobStepContext,
            CurrentUserStepContext currentUserStepContext)
        {
            _publishFundingStepContext = publishFundingStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
            _currentJobStepContext = currentJobStepContext;
            _currentUserStepContext = currentUserStepContext;
        }

        [When(@"funding is published")]
        public async Task WhenFundingIsPublished()
        {
            try
            {
                await _publishFundingStepContext.PublishFunding(
                _currentSpecificationStepContext.SpecificationId,
                _currentJobStepContext.JobId,
                _currentUserStepContext.UserId,
                _currentUserStepContext.UserName);
            }
            catch (System.Exception)
            {
                _publishFundingStepContext.PublishSuccessful = false;
                throw;
            }
            _publishFundingStepContext.PublishSuccessful = true;

        }

        [Then(@"publishing succeeds")]
        public void ThenPublishingSucceeds()
        {
            _publishFundingStepContext.PublishSuccessful
                .Should()
                .BeTrue();
        }

    }
}
