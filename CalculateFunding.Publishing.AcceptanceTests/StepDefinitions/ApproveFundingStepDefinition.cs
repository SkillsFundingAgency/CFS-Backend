using System.Threading.Tasks;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using TechTalk.SpecFlow;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class ApproveFundingStepDefinition
    {
        private readonly IPublishFundingStepContext _publishFundingStepContext;
        private readonly CurrentSpecificationStepContext _currentSpecificationStepContext;
        private readonly CurrentJobStepContext _currentJobStepContext;
        private readonly CurrentUserStepContext _currentUserStepContext;
        private readonly IApproveService _approveService;

        public ApproveFundingStepDefinition(IPublishFundingStepContext publishFundingStepContext,
            CurrentSpecificationStepContext currentSpecificationStepContext,
            CurrentJobStepContext currentJobStepContext,
            CurrentUserStepContext currentUserStepContext, 
            IApproveService approveService)
        {
            _publishFundingStepContext = publishFundingStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
            _currentJobStepContext = currentJobStepContext;
            _currentUserStepContext = currentUserStepContext;
            _approveService = approveService;
        }

        [When(@"funding is approved")]
        public async Task WhenFundingIsApproved()
        {
            Message message = new Message();

            message.UserProperties.Add("user-id", _currentUserStepContext.UserId);
            message.UserProperties.Add("user-name", _currentUserStepContext.UserName);
            message.UserProperties.Add("specification-id", _currentSpecificationStepContext.SpecificationId);
            message.UserProperties.Add("jobId", _currentJobStepContext.JobId);

            await _approveService.ApproveResults(message);
        }
    }
}
