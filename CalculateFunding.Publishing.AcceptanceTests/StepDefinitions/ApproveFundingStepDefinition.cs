using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class ApproveFundingStepDefinition
    {
        private readonly IPublishFundingStepContext _publishFundingStepContext;
        private readonly CurrentSpecificationStepContext _currentSpecificationStepContext;
        private readonly CurrentJobStepContext _currentJobStepContext;
        private readonly CurrentUserStepContext _currentUserStepContext;

        public ApproveFundingStepDefinition(IPublishFundingStepContext publishFundingStepContext,
            CurrentSpecificationStepContext currentSpecificationStepContext,
            CurrentJobStepContext currentJobStepContext,
            CurrentUserStepContext currentUserStepContext)
        {
            _publishFundingStepContext = publishFundingStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
            _currentJobStepContext = currentJobStepContext;
            _currentUserStepContext = currentUserStepContext;
        }

        [When(@"funding is approved")]
        public async Task WhenFundingIsApproved()
        {
            try
            {
                await _publishFundingStepContext.ApproveResults(
                _currentSpecificationStepContext.SpecificationId,
                _currentJobStepContext.JobId,
                _currentUserStepContext.UserId,
                _currentUserStepContext.UserName);
            }
            catch (Exception)
            {
                _publishFundingStepContext.ApproveFundingSuccessful = false;
                throw;
            }
            _publishFundingStepContext.ApproveFundingSuccessful = true;
        }

        [Then(@"approve funding succeeds")]
        public void ThenApproveFundingSucceeds()
        {
            _publishFundingStepContext.ApproveFundingSuccessful
               .Should()
               .BeTrue();
        }
    }
}
