using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class RefreshStepDefinition
    {
        private readonly IPublishFundingStepContext _publishFundingStepContext;
        private readonly CurrentSpecificationStepContext _currentSpecificationStepContext;
        private readonly CurrentJobStepContext _currentJobStepContext;
        private readonly CurrentUserStepContext _currentUserStepContext;

        public RefreshStepDefinition(IPublishFundingStepContext publishFundingStepContext,
            CurrentSpecificationStepContext currentSpecificationStepContext,
            CurrentJobStepContext currentJobStepContext,
            CurrentUserStepContext currentUserStepContext)
        {
            _publishFundingStepContext = publishFundingStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
            _currentJobStepContext = currentJobStepContext;
            _currentUserStepContext = currentUserStepContext;
        }

        [When(@"funding is refreshed")]
        public async Task WhenFundingIsRefreshed()
        {
            try
            {
                await _publishFundingStepContext.RefreshFunding(_currentSpecificationStepContext.SpecificationId,
                _currentJobStepContext.JobId,
                _currentUserStepContext.UserId,
                _currentUserStepContext.UserName);
            }
            catch(System.Exception)
            {
                _publishFundingStepContext.RefreshSuccessful = false;
                throw;
            }

            _publishFundingStepContext.RefreshSuccessful = true;
        }

        [Then(@"refresh succeeds")]
        public void ThenRefreshSucceeds()
        {
            _publishFundingStepContext.RefreshSuccessful
                .Should()
                .BeTrue();
        }
    }
}
